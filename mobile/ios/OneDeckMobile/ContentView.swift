import SwiftUI
import WebViewKit

struct ContentView: View {
    @EnvironmentObject var appState: AppState
    @State private var webView: WebViewWrapper?

    var body: some View {
        ZStack {
            if let webView = webView {
                webView
                    .edgesIgnoringSafeArea(.all)
            } else {
                // 加载中
                ProgressView("Loading OneDeck...")
            }
        }
        .onAppear {
            setupWebView()
        }
    }

    private func setupWebView() {
        let wrapper = WebViewWrapper()
        wrapper.setupBridge(appState: appState)
        webView = wrapper
    }
}

/// WebView 封装，注入 JSAPI Bridge
struct WebViewWrapper: UIViewRepresentable {
    private var bridge: OneDeckJsBridge?

    func makeUIView(context: Context) -> WKWebView {
        let config = WKWebViewConfiguration()
        let contentController = config.userContentController

        // 创建 JSAPI Bridge
        let bridge = OneDeckJsBridge()
        self.bridge = bridge

        // 注入 JSAPI
        contentController.add(bridge, name: "OneDeckNative")

        let webView = WKWebView(frame: .zero, configuration: config)
        webView.navigationDelegate = context.coordinator

        // 加载前端
        if let url = Bundle.main.url(forResource: "index", withExtension: "html", subdirectory: "frontend") {
            webView.loadFileURL(url, allowingReadAccessTo: url.deletingLastPathComponent())
        }

        return webView
    }

    func updateUIView(_ webView: WKWebView, context: Context) {}

    func makeCoordinator() -> Coordinator {
        Coordinator()
    }

    class Coordinator: NSObject, WKNavigationDelegate {
        func webView(_ webView: WKWebView, didFinish navigation: WKNavigation!) {
            // 页面加载完成后初始化 WebSocket
        }
    }
}
