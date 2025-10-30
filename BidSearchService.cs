using Nafes.CrossCutting.Common.OperationResponse;
using Nafis.Services.Contracts;
using Nafis.Services.DTO.Bid;
using Tanafos.Main.Services.DTO.Bid;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nafis.Services.DTO.Provider;

namespace Nafis.Services.Implementation
{
   
    public class BidSearchService : IBidSearchService
    {
        private readonly BidServiceCore _bidServiceCore;

        public BidSearchService(BidServiceCore bidServiceCore)
        {
            _bidServiceCore = bidServiceCore;
        }

        public async Task<PagedResponse<IReadOnlyList<ReadOnlyFilterBidModel>>> GetBidsList(FilterBidsSearchModel request)
            => await _bidServiceCore.GetBidsList(request);

        public async Task<PagedResponse<IReadOnlyList<ReadOnlyFilterBidModel>>> GetBidsCreatedByUser(GetBidsCreatedByUserModel request)
            => await _bidServiceCore.GetBidsCreatedByUser(request);

        public async Task<PagedResponse<IReadOnlyList<ReadOnlyFilterBidModel>>> GetAssociationBids(GetBidsCreatedByUserModel request)
            => await _bidServiceCore.GetAssociationBids(request);

        public async Task<PagedResponse<IReadOnlyList<ReadOnlyPublicBidListModel>>> GetPublicBidsList(int pageSize, int pageNumber)
            => await _bidServiceCore.GetPublicBidsList(pageSize, pageNumber);

        public async Task<PagedResponse<IReadOnlyList<ReadOnlyPublicBidListModel>>> GetPublicFreelancingBidsList(FilterBidsSearchModel request)
            => await _bidServiceCore.GetPublicFreelancingBidsList(request);

        public async Task<PagedResponse<List<GetMyBidResponse>>> GetMyBidsAsync(FilterBidsSearchModel model)
            => await _bidServiceCore.GetMyBidsAsync(model);

        public async Task<PagedResponse<IReadOnlyList<GetProvidersBidsReadOnly>>> GetpProviderBids(int pageSize = 10, int pageNumber = 1)
            => await _bidServiceCore.GetpProviderBids(pageSize, pageNumber);

        public async Task<PagedResponse<IReadOnlyList<GetMyBidResponse>>> GetAllBids(FilterBidsSearchModel request)
            => await _bidServiceCore.GetAllBids(request);

        public async Task<OperationResult<GetBidsSearchHeadersResponse>> GetBidsSearchHeadersAsync()
            => await _bidServiceCore.GetBidsSearchHeadersAsync();

        public async Task<PagedResponse<IReadOnlyList<GetProvidersBidsReadOnly>>> GetAssociationProviderBids(int pageSize = 10, int pageNumber = 1)
            => await _bidServiceCore.GetAssociationProviderBids(pageSize, pageNumber);
    }
}
