using Microsoft.EntityFrameworkCore;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks; // Required for FindAsync

namespace CMCS_Prototype.Tests
{
    // MUST be a public static class
    public static class MockDbSetExtensions
    {
        // MUST be a public static method using 'this IQueryable<T> source'
        public static Mock<DbSet<T>> MockDbSet<T>(this IQueryable<T> source) where T : class
        {
            var mockSet = new Mock<DbSet<T>>();
            var dataList = source.ToList();

            // Setup IQueryable behavior for LINQ queries
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(source.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(source.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(source.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(source.GetEnumerator());

            // Setup FindAsync behavior (Crucial for controller.Approve/Reject)
            mockSet.Setup(x => x.FindAsync(It.IsAny<object[]>()))
                   .Returns<object[]>(ids =>
                       ValueTask.FromResult(
                           dataList.FirstOrDefault(d =>
                               d.GetType().GetProperty("ClaimID")?
                               .GetValue(d)?
                               .Equals(ids[0]) == true)
                       )
                   );

            return mockSet;
        }
    }
}