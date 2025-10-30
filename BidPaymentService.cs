using Nafes.CrossCutting.Common.OperationResponse;
using Nafis.Services.Contracts;
using Nafis.Services.DTO.Bid;
using Tanafos.Main.Services.DTO.Bid;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nafis.Services.DTO.BuyTenderDocsPill;
using Nafis.Services.DTO.Provider;

namespace Nafis.Services.Implementation
{
  
    public class BidPaymentService : IBidPaymentService
    {
        private readonly BidServiceCore _bidServiceCore;

        public BidPaymentService(BidServiceCore bidServiceCore)
        {
            _bidServiceCore = bidServiceCore;
        }

        public async Task<OperationResult<ReadOnlyGetBidPriceModel>> GetBidPrice(GetBidDocumentsPriceRequestModel request)
            => await _bidServiceCore.GetBidPrice(request);

        public async Task<OperationResult<ReadOnlyGetBidPriceModel>> GetBidPriceForFreelancer(GetBidDocumentsPriceRequestModel request)
            => await _bidServiceCore.GetBidPriceForFreelancer(request);

        public async Task<OperationResult<BuyTermsBookResponseModel>> BuyTermsBook(BuyTermsBookModel model)
            => await _bidServiceCore.BuyTermsBook(model);

        public async Task<OperationResult<BuyTenderDocsPillModel>> GetBuyTenderDocsPillModel(long providerBidId)
            => await _bidServiceCore.GetBuyTenderDocsPillModel(providerBidId);

        public async Task<OperationResult<GetProviderDataOfRefundableCompanyBidModel>> GetProviderDataOfRefundableCompanyBid(long companyBidId)
            => await _bidServiceCore.GetProviderDataOfRefundableCompanyBid(companyBidId);

        public async Task<OperationResult<List<GetCompaniesToBuyTermsBookResponse>>> GetCurrentUserCompaniesToBuyTermsBookWithForbiddenReasonsIfFoundAsync(long bidId, long? currenctUserSpecificCompanyId = null)
            => await _bidServiceCore.GetCurrentUserCompaniesToBuyTermsBookWithForbiddenReasonsIfFoundAsync(bidId, currenctUserSpecificCompanyId);

        public async Task<OperationResult<GetFreelancersToBuyTermsBookResponse>> GetCurrentUserFreelancersToBuyTermsBookWithForbiddenReasonsIfFoundAsync(long bidId, long? freelancerId)
            => await _bidServiceCore.GetCurrentUserFreelancersToBuyTermsBookWithForbiddenReasonsIfFoundAsync(bidId, freelancerId);
    }
}
