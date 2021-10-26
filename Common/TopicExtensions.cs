namespace Common
{
    public static class TopicExtensions
    {
        public static string GetTopicName(this Topic topic)
            => topic.ToString()
                .Replace("_", "-")
                .ToLowerInvariant();
    }
}
