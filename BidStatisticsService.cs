using Nafes.CrossCutting.Common.OperationResponse;
using Nafes.CrossCutting.Common.Security;
using Nafes.CrossCutting.Model.Entities;
using Nafis.Services.Contracts;
using Nafis.Services.DTO.AssociationWithdraw;
using Nafis.Services.DTO.Bid;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tanafos.Main.Services.DTO.Bid;

namespace Nafis.Services.Implementation
{
    
    public class BidStatisticsService : IBidStatisticsService
    {
        private readonly BidServiceCore _bidServiceCore;

        public BidStatisticsService(BidServiceCore bidServiceCore)
        {
            _bidServiceCore = bidServiceCore;
        }

        public async Task<OperationResult<long>> IncreaseBidViewCount(long bidId)
        {
            return await _bidServiceCore.IncreaseBidViewCount(bidId);
        }

        public async Task<PagedResponse<List<GetBidViewsModel>>> GetBidViews(long bidId, int pageSize, int pageNumber)
        {
            return await _bidServiceCore.GetBidViews(bidId, pageSize, pageNumber);
        }

        public async Task<OperationResult<BidViewsStatisticsResponse>> GetBidViewsStatisticsAsync(long bidId)
        {
            return await _bidServiceCore.GetBidViewsStatisticsAsync(bidId);
        }

        public long GetBidCreatorId(Bid bid) { 
            return _bidServiceCore.GetBidCreatorId(bid);
        }



        public async Task<List<ProviderBid>> GetProviderBidsWithAssociationFees(List<ProvidersBidsWithdrawModel> providerBidsIds, long creatorId, UserType creatorType)
        {
                        return await _bidServiceCore.GetProviderBidsWithAssociationFees(providerBidsIds, creatorId, creatorType);

        }


        public (decimal, decimal) GetTanafosAssociationFeesOfBoughtTermsBooks(IEnumerable<ProviderBid> pbs) { 
            return _bidServiceCore.GetTanafosAssociationFeesOfBoughtTermsBooks(pbs);
        }


        public (decimal, decimal) GetTanafosAssociationFeesOfBoughtTermsBook(ProviderBid pb)
        {
            return _bidServiceCore.GetTanafosAssociationFeesOfBoughtTermsBook(pb);
        }
        


    }
}
