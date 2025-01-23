namespace AlertClient.Models;

public record AlertData(string RowKey,
    string PartitionKey,
    int Value);
