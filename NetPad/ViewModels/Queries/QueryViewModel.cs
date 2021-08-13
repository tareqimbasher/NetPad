using NetPad.Queries;

namespace NetPad.ViewModels.Queries
{
    public class QueryViewModel : ViewModelBase
    {
        public QueryViewModel(Query query)
        {
            Query = query;
        }

        public Query Query { get; }
    }
}