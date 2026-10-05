interface ISummarizer
{
    Task<string?> SummarizeAsync(List<Item> items, string focus);
}