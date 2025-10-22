using Microsoft.EntityFrameworkCore;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks; // Required for FindAsync

namespace CMCS_Prototype.Tests
{
    public static class MockDbSetExtensions
    {
        // CRITICAL: Updated method signature to use 'source' for clarity
        public static Mock<DbSet<T>> MockDbSet<T>(this IQueryable<T> source) where T : class
        {
            var mockSet = new Mock<DbSet<T>>();
            var dataList = source.ToList(); // Convert IQueryable to List for Moq's Find logic

            // 1. Setup IQueryable behavior for LINQ queries (which you already had)
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(source.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(source.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(source.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(source.GetEnumerator());

            // 2. CRITICAL FIX: Setup FindAsync behavior (Required by Approve/Reject actions)
            // This simulates searching the mock data list by the primary key (assuming it's named 'ClaimID').
            mockSet.Setup(x => x.FindAsync(It.IsAny<object[]>()))
                   .Returns<object[]>(async ids =>
                       dataList.FirstOrDefault(d =>
                           d.GetType().GetProperty("ClaimID")? // Check for 'ClaimID' property
                           .GetValue(d)
                           .Equals(ids[0]) == true)
                   );

            return mockSet;
        }
    }
}