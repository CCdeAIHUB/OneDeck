import Foundation
import WebKit

/**
 * JSAPI Bridge
 * 暴露设备能力给前端 WebView
 * 前端通过 window.OneDeckNative.callApi(apiName, args) 调用
 *
 * 安全限制：只暴露读取能力，不暴露写入/修改系统状态的能力
 */
class OneDeckJsBridge: NSObject, WKScriptMessageHandler {
    private let logBuffer = LogBuffer(maxSize: 1000)

    func userContentController(_ userContentController: WKUserContentController, didReceive message: WKScriptMessage) {
        guard message.name == "OneDeckNative" else { return }

        guard let body = message.body as? [String: Any],
              let method = body["method"] as? String else {
            return
        }

        switch method {
        case "callApi":
            handleCallApi(body)
        case "getDeviceId":
            handleGetDeviceId(message.webView)
        case "getPlatform":
            handleGetPlatform(message.webView)
        case "requestPermission":
            handleRequestPermission(body)
        case "log":
            handleLog(body)
        default:
            break
        }
    }

    // MARK: - JSAPI 实现

    private func handleCallApi(_ body: [String: Any]) {
        guard let apiName = body["apiName"] as? String,
              let argsJson = body["args"] as? String else { return }

        let args = parseJSON(argsJson) ?? [:]
        var result: [String: Any] = ["success": true]

        switch apiName {
        case "device.getInfo":
            result["data"] = [
                "platform": "ios",
                "osVersion": UIDevice.current.systemVersion,
                "model": UIDevice.current.model,
                "screenResolution": [
                    "width": UIScreen.main.bounds.width,
                    "height": UIScreen.main.bounds.height
                ]
            ]

        case "device.getBattery":
            // TODO: UIDevice.current.batteryLevel
            result["data"] = ["level": 100, "charging": false]

        case "device.vibrate":
            // TODO: UIImpactFeedbackGenerator
            break

        case "clipboard.read":
            // TODO: UIPasteboard
            result["data"] = ["text": ""]

        case "clipboard.write":
            // TODO: UIPasteboard
            break

        case "network.getStatus":
            // TODO: NWPathMonitor
            result["data"] = ["connected": true, "type": "wifi"]

        case "file.read", "file.write", "file.delete", "file.exists", "file.list":
            // TODO: 在插件沙箱目录操作
            break

        case "camera.takePhoto", "camera.startStream", "camera.stopStream":
            // TODO: AVFoundation
            break

        case "location.getCurrent", "location.startWatch", "location.stopWatch":
            // TODO: CoreLocation
            break

        case "storage.get", "storage.set", "storage.remove", "storage.keys", "storage.clear":
            // TODO: SQLite 插件数据
            break

        default:
            result = ["success": false, "error": "Unknown API: \(apiName)"]
        }
    }

    private func handleGetDeviceId(_ webView: WKWebView?) {
        let deviceId = UIDevice.current.identifierForVendor?.uuidString ?? ""
        webView?.evaluateJavaScript("window.__onedeck_deviceId = '\(deviceId)'")
    }

    private func handleGetPlatform(_ webView: WKWebView?) {
        webView?.evaluateJavaScript("window.__onedeck_platform = 'ios'")
    }

    private func handleRequestPermission(_ body: [String: Any]) {
        // TODO: 请求权限并记录日志
    }

    private func handleLog(_ body: [String: Any]) {
        let level = body["level"] as? String ?? "info"
        let message = body["message"] as? String ?? ""

        let logEntry: [String: Any] = [
            "level": level,
            "message": message,
            "timestamp": Int(Date().timeIntervalSince1970 * 1000)
        ]

        if let jsonData = try? JSONSerialization.data(withJSONObject: logEntry),
           let jsonStr = String(data: jsonData, encoding: .utf8) {
            logBuffer.add(jsonStr)
        }

        // 如果已连接，立即发送到桌面端
        // TODO: 通过 WebSocket 发送
    }

    // MARK: - 辅助

    private func parseJSON(_ str: String) -> [String: Any]? {
        guard let data = str.data(using: .utf8) else { return nil }
        return try? JSONSerialization.jsonObject(with: data) as? [String: Any]
    }
}

/// 日志环形缓冲区
class LogBuffer {
    private let maxSize: Int
    private var buffer: [String] = []

    init(maxSize: Int = 1000) {
        self.maxSize = maxSize
    }

    func add(_ entry: String) {
        if buffer.count >= maxSize {
            buffer.removeFirst()
        }
        buffer.append(entry)
    }

    func drain() -> [String] {
        let logs = buffer
        buffer.removeAll()
        return logs
    }
}
