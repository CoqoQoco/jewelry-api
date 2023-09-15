using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Reflection;

namespace Kendo.DynamicLinqCore
{
    public static class EnumerableExtensions
    {
        public static dynamic GroupByMany<TElement>(this IEnumerable<TElement> elements, IEnumerable<Group> groupSelectors)
        {
            // Create a new list of Kendo Group Selectors 
            var selectors = new List<GroupSelector<TElement>>(groupSelectors.Count());
            foreach (var selector in groupSelectors)
            {
                // Compile the Dynamic Expression Lambda for each one
                var expression = DynamicExpressionParser.ParseLambda(false, typeof(TElement), typeof(object), selector.Field);

                // Add it to the list
                selectors.Add(new GroupSelector<TElement>
                {
                    Selector = (Func<TElement, object>)expression.Compile(),
                    Field = selector.Field,
                    Aggregates = selector.Aggregates
                });
            }

            // Call the actual group by method
            return elements.GroupByMany(selectors.ToArray());
        }

        public static dynamic GroupByMany<TElement>(this IEnumerable<TElement> elements, params GroupSelector<TElement>[] groupSelectors)
        {
            if (groupSelectors.Length > 0)
            {
                // Get selector
                var selector = groupSelectors[0];
                var nextSelectors = groupSelectors.Skip(1).ToArray();   // Reduce the list recursively until zero

                // Group by and return                
                return  elements.GroupBy(selector.Selector).Select(
                            g => new GroupResult
                            {
                                Value = g.Key,
                                Aggregates = QueryableExtensions.Aggregates(g.AsQueryable(), selector.Aggregates),
                                HasSubgroups = groupSelectors.Length > 1,
                                Count = g.Count(),
                                Items = g.GroupByMany(nextSelectors),   // Recursivly group the next selectors
                                SelectorField = selector.Field
                            });
            }

            // If there are not more group selectors return data
            return elements;
        }

        public static DataSourceResult ToDataSourceResult<T>(this IEnumerable<T> queryable, int take, int skip, IEnumerable<Sort> sort, IEnumerable<Group> group)
        {
            var errors = new List<object>();

            // Filter the data first
            //queryable = Filters(queryable, filter, errors);

            // Calculate the total number of records (needed for paging)            
            var total = queryable.Count();

            // Calculate the aggregates
            //var aggregate = Aggregates(queryable, aggregates);

            if (group?.Any() == true)
            {
                //if(sort == null) sort = GetDefaultSort(queryable.ElementType, sort);
                if (sort == null) sort = new List<Sort>();

                foreach (var source in group.Reverse())
                {
                    sort = sort.Append(new Sort
                    {
                        Field = source.Field,
                        Dir = source.Dir
                    });
                }
            }

            // Sort the data
            queryable = Sort(queryable, sort);

            // Finally page the data
            if (take > 0)
            {
                queryable = Page(queryable, take, skip);
            }

            var result = new DataSourceResult
            {
                Total = total,
                //Aggregates = aggregate
            };

            // Group By
            if (group?.Any() == true)
            {
                //result.Groups = queryable.ToList().GroupByMany(group);                
                result.Groups = queryable.GroupByMany(group);
            }
            else
            {
                result.Data = queryable.ToList();
            }

            // Set errors if any
            if (errors.Count > 0)
            {
                result.Errors = errors;
            }

            return result;
        }

        private static IEnumerable<T> Sort<T>(IEnumerable<T> enumerable, IEnumerable<Sort> sort)
        {
            if (sort?.Any() == true)
            {
                // Create ordering expression e.g. Field1 asc, Field2 desc
                var ordering = string.Join(",", sort.Select(s => s.ToExpression()));

                // Use the OrderBy method of Dynamic Linq to sort the data
                return enumerable.AsQueryable().OrderBy(ordering);
            }

            return enumerable;
        }


        private static IEnumerable<T> Page<T>(IEnumerable<T> queryable, int take, int skip)
        {
            return queryable.Skip(skip).Take(take);
        }

        /// <summary>
        /// Pretreatment of specific DateTime type and convert some illegal value type
        /// </summary>
        /// <param name="filter"></param>
        private static Filter PreliminaryWork(Type type, Filter filter)
        {
            if (filter.Filters != null && filter.Logic != null)
            {
                var newFilters = new List<Filter>();
                foreach (var f in filter.Filters)
                {
                    newFilters.Add(PreliminaryWork(type, f));
                }

                filter.Filters = newFilters;
            }

            if (filter.Value == null) return filter;

            // When we have a decimal value, it gets converted to an integer/double that will result in the query break
            var currentPropertyType = Filter.GetLastPropertyType(type, filter.Field);
            if ((currentPropertyType == typeof(decimal) || currentPropertyType == typeof(decimal?)) && decimal.TryParse(filter.Value.ToString(), out decimal number))
            {
                filter.Value = number;
                return filter;
            }

            // if(currentPropertyType.GetTypeInfo().IsEnum && int.TryParse(filter.Value.ToString(), out int enumValue))
            // {           
            //     filter.Value = Enum.ToObject(currentPropertyType, enumValue);
            //     return filter;
            // }

            // Convert datetime-string to DateTime
            if (currentPropertyType == typeof(DateTime) && DateTime.TryParse(filter.Value.ToString(), out DateTime dateTime))
            {
                filter.Value = dateTime;

                // Copy the time from the filter
                var localTime = dateTime.ToLocalTime();

                // Used when the datetime's operator value is eq and local time is 00:00:00 
                if (filter.Operator == "eq")
                {
                    if (localTime.Hour != 0 || localTime.Minute != 0 || localTime.Second != 0)
                        return filter;

                    var newFilter = new Filter { Logic = "and" };
                    newFilter.Filters = new List<Filter>
                    {
                        // Instead of comparing for exact equality, we compare as greater than the start of the day...
                        new Filter
                        {
                            Field = filter.Field,
                            Filters = filter.Filters,
                            Value = new DateTime(localTime.Year, localTime.Month, localTime.Day, 0, 0, 0),
                            Operator = "gte"
                        },
                        // ...and less than the end of that same day (we're making an additional filter here)
                        new Filter
                        {
                            Field = filter.Field,
                            Filters = filter.Filters,
                            Value = new DateTime(localTime.Year, localTime.Month, localTime.Day, 23, 59, 59),
                            Operator = "lte"
                        }
                    };

                    return newFilter;
                }

                // Convert datetime to local 
                filter.Value = new DateTime(localTime.Year, localTime.Month, localTime.Day, localTime.Hour, localTime.Minute, localTime.Second, localTime.Millisecond);
            }

            return filter;
        }
    }
}