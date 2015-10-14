using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Contractual
{
    public enum SortDirection
    {
        ASC,
        DESC
    }

    public class SortSpec
    {
        public string Property { get; set; }
        public SortDirection Direction { get; set; }

        public SortSpec(string property, SortDirection direction)
        {
            Property = property;
            Direction = direction;
        }
    }

    public static class Sortation
    {
        public static OrderedSortation<T> OrderBy<T, TKey>(this T data, Expression<Func<T, TKey>> sort) where T : IQuerySpec
        {
            var propInfo = OrderedSortation<T>.VerifyParams(sort);
            var sortParams = new OrderedSortation<T>();
            sortParams.Add(propInfo.Name, SortDirection.ASC);

            return sortParams;
        }
    }

    public class Sortation<T>
    {
        public static OrderedSortation<T> OrderBy<TKey>(Expression<Func<T, TKey>> sort) where TKey : IQuerySpec
        {
            var propInfo = OrderedSortation<T>.VerifyParams(sort);
            var sortParams = new OrderedSortation<T>();
            sortParams.Add(propInfo.Name, SortDirection.ASC);

            return sortParams;
        }
    }

    public class OrderedSortation<T>
    {
        private List<SortSpec> sortation = new List<SortSpec>();

        internal void Add(string name, SortDirection sort)
        {
            sortation.Add(new SortSpec(name, sort));
        }

        internal OrderedSortation() { }

        public List<SortSpec> ToList()
        {
            return sortation;
        }

        public OrderedSortation<T> ThenBy<TKey>(Expression<Func<T, TKey>> addSort)
        {
            var propInfo = VerifyParams(addSort);
            Add(propInfo.Name, SortDirection.ASC);
            return this;
        }

        public OrderedSortation<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> addSort)
        {
            var propInfo = VerifyParams(addSort);
            Add(propInfo.Name, SortDirection.DESC);
            return this;
        }

        internal static PropertyInfo VerifyParams<TKey>(Expression<Func<T, TKey>> sort)
        {
            var member = sort.Body as MemberExpression;
            if (member == null)
            {
                throw new ArgumentException(string.Format(
                    "Expression '{0}' does not refer to a property.",
                    sort.ToString()));
            }

            PropertyInfo propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a field, not a property.",
                    propInfo.ToString()));

            return propInfo;
        }
    }
}
