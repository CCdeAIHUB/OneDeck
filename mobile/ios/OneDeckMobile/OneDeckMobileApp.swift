import SwiftUI

@main
struct OneDeckMobileApp: App {
    @StateObject private var appState = AppState()

    var body: some Scene {
        WindowGroup {
            ContentView()
                .environmentObject(appState)
        }
    }
}

class AppState: ObservableObject {
    @Published var isConnected = false
    @Published var desktopUrl = "ws://192.168.1.100:9720/mobile"
    @Published var deviceId = ""
}
