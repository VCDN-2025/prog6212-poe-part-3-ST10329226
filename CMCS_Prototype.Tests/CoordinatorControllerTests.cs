using CMCS_Prototype.Controllers;
using CMCS_Prototype.Data;
using CMCS_Prototype.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using CMCS_Prototype.Tests;

namespace CMCS_Prototype.Tests
{
    public class CoordinatorControllerTests
    {
        private Mock<CMCSDbContext> GetMockContext(List<Claim> claims)
        {
            var mockSet = claims.AsQueryable().MockDbSet();

            var mockContext = new Mock<CMCSDbContext>();
            mockContext.Setup(c => c.Claims).Returns(mockSet.Object);
            mockContext.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

            return mockContext;
        }

        // TEST 1: Successful Approval
        [Fact]
        public async Task Approve_ValidId_SetsStatusToCoordinatorApprovedAndRedirects()
        {
            // ARRANGE
            var claimToApprove = new Claim { ClaimID = 1, Status = "Pending", CoordinatorID = null };
            var mockClaims = new List<Claim> { claimToApprove };
            var mockContext = GetMockContext(mockClaims);
            var controller = new CoordinatorController(mockContext.Object);

            // ACT
            var result = await controller.Approve(1);

            // ASSERT
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("PendingClaims", redirectToActionResult.ActionName);
            Assert.Equal("Coordinator Approved", claimToApprove.Status);
            mockContext.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        // TEST 2: Successful Rejection
        [Fact]
        public async Task Reject_ValidIdAndReason_SetsStatusToRejectedAndRedirects()
        {
            // Change this line:
            // var claimToReject = new Claim { ClaimID = 2, Status = "Pending", RejectionReason = null };

            // To this:
            var claimToReject = new Claim { ClaimID = 2, Status = "Pending", RejectionReason = string.Empty };
            var mockClaims = new List<Claim> { claimToReject };
            var mockContext = GetMockContext(mockClaims);
            var controller = new CoordinatorController(mockContext.Object);
            var reason = "Incomplete supporting documentation.";

            // ACT
            var result = await controller.Reject(2, reason);

            // ASSERT
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("PendingClaims", redirectToActionResult.ActionName);
            Assert.Equal("Rejected", claimToReject.Status);
            Assert.Equal(reason, claimToReject.RejectionReason);
            mockContext.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        // TEST 3: Approval of a Non-Existent Claim (Error Handling)
        [Fact]
        public async Task Approve_InvalidId_ReturnsNotFound()
        {
            // ARRANGE
            var mockClaims = new List<Claim>();
            var mockContext = GetMockContext(mockClaims);
            var controller = new CoordinatorController(mockContext.Object);

            // ACT
            var result = await controller.Approve(99);

            // ASSERT
            Assert.IsType<NotFoundResult>(result);
        }

        // TEST 4: Database Failure Handling
        [Fact]
        public async Task Approve_ThrowsException_SetsTempDataAndRedirects()
        {
            // ARRANGE
            var claimToApprove = new Claim { ClaimID = 3, Status = "Pending" };
            var mockClaims = new List<Claim> { claimToApprove };
            var mockContext = GetMockContext(mockClaims);

            mockContext.Setup(c => c.SaveChangesAsync(default)).ThrowsAsync(new DbUpdateException("Simulated DB Lockout"));

            var controller = new CoordinatorController(mockContext.Object)
            {
                TempData = new Mock<ITempDataDictionary>().Object
            };

            // ACT
            var result = await controller.Approve(3);

            // ASSERT
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("PendingClaims", redirectToActionResult.ActionName);
            Assert.Equal("Pending", claimToApprove.Status); // Status must not have changed
        }
    }
}