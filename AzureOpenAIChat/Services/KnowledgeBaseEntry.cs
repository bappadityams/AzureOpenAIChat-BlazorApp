using Azure.Search.Documents.Indexes;

namespace AzureOpenAIChat.Services
{
    public class KnowledgeBaseEntry
    {
        [SimpleField(IsKey = true)]
        public string? id { get; set; }
        [SearchableField]
        public string? content { get; set; }       
        [SearchableField]
        public string? group_id { get; set; }
        
    }
}
