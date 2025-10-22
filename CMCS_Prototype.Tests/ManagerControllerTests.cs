using CMCS_Prototype.Controllers;
using CMCS_Prototype.Data;
using CMCS_Prototype.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CMCS_Prototype.Tests
{
    public class ManagerControllerTests
    {
        // NOTE: This helper relies on MockDbSetExtensions.cs and CMCSDbContext having a protected parameterless constructor.
        private Mock<CMCSDbContext> GetMockContext(List<Claim> claims)
        {
            var mockSet = claims.AsQueryable().MockDbSet();

            var mockContext = new Mock<CMCSDbContext>();
            mockContext.Setup(c => c.Claims).Returns(mockSet.Object);
            mockContext.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

            return mockContext;
        }

        // TEST 1: Successful Final Approval 
        [Fact]
        public async Task Approve_ValidCoordinatorApprovedClaim_SetsStatusToSettledAndRedirects()
        {
            // ARRANGE
            var claimToApprove = new Claim { ClaimID = 1, Status = "Coordinator Approved", ManagerID = null };
            var mockClaims = new List<Claim> { claimToApprove };
            var mockContext = GetMockContext(mockClaims);
            var controller = new ManagerController(mockContext.Object);

            // ACT
            var result = await controller.Approve(1);

            // ASSERT
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("PendingClaims", redirectToActionResult.ActionName);
            // Verify status matches the controller's "Settled" status
            Assert.Equal("Settled", claimToApprove.Status);
            mockContext.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        // TEST 2: Successful Final Rejection (Saves Reason)
        [Fact]
        public async Task Reject_ValidIdAndReason_SetsStatusToManagerRejectedAndSavesReason()
        {
            // ARRANGE
            var claimToReject = new Claim { ClaimID = 2, Status = "Coordinator Approved", RejectionReason = string.Empty };
            var mockClaims = new List<Claim> { claimToReject };
            var mockContext = GetMockContext(mockClaims);
            var controller = new ManagerController(mockContext.Object);
            var reason = "Budget exceeded.";

            // ACT
            var result = await controller.Reject(2, reason);

            // ASSERT
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("PendingClaims", redirectToActionResult.ActionName);
            Assert.Equal("Manager Rejected", claimToReject.Status);
            Assert.Equal(reason, claimToReject.RejectionReason);
            mockContext.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        // TEST 3: Attempt to Approve a Claim Not Ready for Manager Review (Now passes due to controller fix)
        [Fact]
        public async Task Approve_PendingStatusClaim_ReturnsRedirectWithoutChangingStatus()
        {
            // ARRANGE
            var claimToApprove = new Claim { ClaimID = 3, Status = "Pending" };
            var mockClaims = new List<Claim> { claimToApprove };
            var mockContext = GetMockContext(mockClaims);
            var controller = new ManagerController(mockContext.Object)
            {
                // Must mock TempData since the controller now uses it in the if-check
                TempData = new Mock<ITempDataDictionary>().Object
            };

            // ACT
            var result = await controller.Approve(3);

            // ASSERT
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("PendingClaims", redirectToActionResult.ActionName);
            // Status MUST remain unchanged because of the new 'if' block
            Assert.Equal("Pending", claimToApprove.Status);
            // SaveChangesAsync MUST NOT be called because the code returns early
            mockContext.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }
    }
}