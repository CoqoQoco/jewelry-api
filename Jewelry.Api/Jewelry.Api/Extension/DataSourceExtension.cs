using Kendo.DynamicLinqCore;

namespace Jewelry.Api.Extension
{
    public static class DataSourceExtension
    {
        public static DataSourceResult ToDataSource<T>(this IQueryable<T> query, DataSourceRequest request)
        {
            return query.ToDataSourceResult(request.Take,
                                            request.Skip,
                                            request.Sort,
                                            request.Filter,
                                            request.Aggregate,
                                            request.Group);
        }

        public static DataSourceResult ToDataSource<T>(this IEnumerable<T> query, DataSourceRequest request)
        {
            return query.ToDataSourceResult(request.Take,
                                            request.Skip,
                                            request.Sort,
                                            request.Group);
        }
    }
}
