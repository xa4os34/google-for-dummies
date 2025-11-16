public static class MessagePriorityLevelExtensions
{
	public static string GetQueueName(this MessagePriorityLevel level, string baseQueueName)
	{
		return $"{baseQueueName}-{System.Enum.GetName(level)}";
	}
}


