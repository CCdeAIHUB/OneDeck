package ai.onedeck.mobile

/**
 * 日志环形缓冲区
 * 断连期间缓存日志，重连后批量补传
 * 最大容量 1000 条，溢出时丢弃最旧日志
 */
class LogBuffer(private val maxSize: Int = 1000) {
    private val buffer = ArrayDeque<String>(maxSize)

    @Synchronized
    fun add(logJson: String) {
        if (buffer.size >= maxSize) {
            buffer.removeFirst()
        }
        buffer.addLast(logJson)
    }

    @Synchronized
    fun drain(): List<String> {
        val logs = buffer.toList()
        buffer.clear()
        return logs
    }

    @Synchronized
    fun size(): Int = buffer.size

    @Synchronized
    fun clear() {
        buffer.clear()
    }
}
