using Nafes.CrossCutting.Common.OperationResponse;
using Nafes.CrossCutting.Model.Entities;
using Nafis.Services.Contracts;
using Tanafos.Main.Services.DTO.Bid;
using Tanafos.Main.Services.DTO.BidAddresses;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tanafos.Main.Services.DTO;
using Nafis.Services.DTO.Bid;

namespace Nafis.Services.Implementation
{
    
    public class BidManagementService : IBidManagementService
    {
        private readonly BidServiceCore _bidServiceCore;

        public BidManagementService(BidServiceCore bidServiceCore)
        {
            _bidServiceCore = bidServiceCore;
        }

        public async Task<OperationResult<ReadOnlyBidModel>> GetBidDetails(long id)
            => await _bidServiceCore.GetBidDetails(id);

        public async Task<OperationResult<ReadOnlyBidMainDataModel>> GetBidMainData(long id)
            => await _bidServiceCore.GetBidMainData(id);

        public async Task<OperationResult<ReadOnlyBidResponse>> GetDetailsForBidByIdAsync(long bidId)
            => await _bidServiceCore.GetDetailsForBidByIdAsync(bidId);

        public async Task<OperationResult<ReadOnlyPublicBidModel>> GetPublicBidDetails(long id)
            => await _bidServiceCore.GetPublicBidDetails(id);

        public async Task<OperationResult<GetBidDetailsForShare>> GetBidDetailsForShare(long bidId)
            => await _bidServiceCore.GetBidDetailsForShare(bidId);

        public async Task<OperationResult<ReadOnlyBidAddressesTimeModel>> GetBidAddressesTime(long bidId)
            => await _bidServiceCore.GetBidAddressesTime(bidId);

        public async Task<OperationResult<ReadOnlyBidAttachmentRequest>> GetBidAttachment(long bidId)
            => await _bidServiceCore.GetBidAttachment(bidId);

        public async Task<OperationResult<IReadOnlyList<ReadOnlyBidNewsModel>>> GetBidNews(long bidId)
            => await _bidServiceCore.GetBidNews(bidId);

        public async Task<OperationResult<List<ReadOnlyQuantitiesTableModel>>> GetBidQuantitiesTable(long bidId)
            => await _bidServiceCore.GetBidQuantitiesTable(bidId);

        public async Task<PagedResponse<List<ReadOnlyQuantitiesTableModel>>> GetBidQuantitiesTableNew(long bidId, int pageSize = 5, int pageNumber = 1)
            => await _bidServiceCore.GetBidQuantitiesTableNew(bidId, pageSize, pageNumber);

        public async Task<(OperationResult<byte[]>, string)> GetZipFileForBidAttachmentAsBinary(long bidId)
            => await _bidServiceCore.GetZipFileForBidAttachmentAsBinary(bidId);

        public async Task<OperationResult<ReadOnlyBidStatusDetailsModel>> GetBidStatusDetails(long bidId)
            => await _bidServiceCore.GetBidStatusDetails(bidId);

        public async Task<OperationResult<List<BidStatusResponse>>> GetBidStatusWithDates(long bidId)
            => await _bidServiceCore.GetBidStatusWithDates(bidId);

        public async Task<OperationResult<List<BidStatusResponse>>> GetBidStatusWithDatesForBid(Bid bidInDb)
            => await _bidServiceCore.GetBidStatusWithDatesForBid(bidInDb);

        public async Task<OperationResult<List<BidStatusResponse>>> GetBidStatusWithDatesForInstantBids(Bid bidInDb)
            => await _bidServiceCore.GetBidStatusWithDatesForInstantBids(bidInDb);

        public async Task<List<BidStatusResponse>> OrderTimelineByIndexIfIgnoreTimelineIsTrue(List<BidStatusResponse> model)
            => await _bidServiceCore.OrderTimelineByIndexIfIgnoreTimelineIsTrue(model);

        public async Task<OperationResult<bool>> UpdateReadProviderRead(long id)
            => await _bidServiceCore.UpdateReadProviderRead(id);

        public async Task<OperationResult<bool>> IsBidInEvaluation(long bidId)
            => await _bidServiceCore.IsBidInEvaluation(bidId);

        public async Task<OperationResult<bool>> ToggelsAbleToSubscribeToBid(long bidId)
            => await _bidServiceCore.ToggelsAbleToSubscribeToBid(bidId);

        public async Task<OperationResult<bool>> RevealBidInCaseNotSubscribe(long bidId)
            => await _bidServiceCore.RevealBidInCaseNotSubscribe(bidId);

        public async Task<OperationResult<long>> AddRFIandRequests(AddRFIRequestModel model)
            => await _bidServiceCore.AddRFIandRequests(model);

        public async Task<OperationResult<IReadOnlyList<ReadOnlyBidRFiRequestModel>>> GetBidRFiRequests(long bidId, int typeId)
            => await _bidServiceCore.GetBidRFiRequests(bidId, typeId);

        public async Task<OperationResult<GetUserRoleResponse>> GetUserRole()
            => await _bidServiceCore.GetUserRole();

        public async Task<OperationResult<int>> GetStoppingPeriod()
            => await _bidServiceCore.GetStoppingPeriod();

        public async Task<OperationResult<QuantityStableSettings>> QuantityStableSettings()
            => await _bidServiceCore.QuantityStableSettings();

        public async Task<OperationResult<GetEntityContactDetailsResponse>> GetEntityContactDetails(GetEntityContactDetailsRequest request)
            => await _bidServiceCore.GetEntityContactDetails(request);

        public async Task<string> GetBidCreatorName(Bid bid)
            => await _bidServiceCore.GetBidCreatorName(bid);

        public async Task<string> GetBidCreatorEmailToReceiveEmails(Bid bid)
            => await _bidServiceCore.GetBidCreatorEmailToReceiveEmails(bid);

        public async Task<(string, string)> GetBidCreatorImage(Bid bid)
            => await _bidServiceCore.GetBidCreatorImage(bid);
    }
}
