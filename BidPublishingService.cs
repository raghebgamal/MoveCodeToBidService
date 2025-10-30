using Nafes.CrossCutting.Common.OperationResponse;
using Nafes.CrossCutting.Model.Entities;
using Nafes.CrossCutting.Model.Enums;
using Nafis.Services.Contracts;
using Tanafos.Main.Services.DTO.Bid;
using System.Threading.Tasks;
using Nafes.CrossCutting.Common.Security;
using Nafis.Services.DTO.Bid;

namespace Nafis.Services.Implementation
{
    
    public class BidPublishingService : IBidPublishingService
    {
        private readonly BidServiceCore _bidServiceCore;

        public BidPublishingService(BidServiceCore bidServiceCore)
        {
            _bidServiceCore = bidServiceCore;
        }

        public async Task<OperationResult<bool>> TakeActionOnPublishingBidByAdmin(PublishBidDto request)
            => await _bidServiceCore.TakeActionOnPublishingBidByAdmin(request);

        public async Task ExecutePostPublishingLogic(Bid bid, ApplicationUser usr, TenderStatus oldStatusOfBid)
            => await _bidServiceCore.ExecutePostPublishingLogic(bid, usr, oldStatusOfBid);

        public async Task<OperationResult<bool>> TakeActionOnBidByDonor(long bidDonorId, DonorResponse donorResponse)
            => await _bidServiceCore.TakeActionOnBidByDonor(bidDonorId, donorResponse);

        public async Task<OperationResult<bool>> TakeActionOnBidSubmissionBySupervisingBid(BidSupervisingActionRequest req)
            => await _bidServiceCore.TakeActionOnBidSubmissionBySupervisingBid(req);

        public async Task SendEmailAndNotifyDonor(Bid bid)
            => await _bidServiceCore.SendEmailAndNotifyDonor(bid);

        public async Task SendUpdatedBidEmailToCreatorAndProvidersOfThisBid(Bid bid)
            => await _bidServiceCore.SendUpdatedBidEmailToCreatorAndProvidersOfThisBid(bid);
    }
}
