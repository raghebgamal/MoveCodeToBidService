using Microsoft.AspNetCore.Http;
using Nafes.CrossCutting.Common.OperationResponse;
using Nafes.CrossCutting.Model.Entities;
using Nafes.CrossCutting.Model.Enums;
using Nafis.Services.Contracts;
using Nafis.Services.DTO.Bid;
using Tanafos.Main.Services.DTO.Bid;
using Tanafos.Main.Services.DTO.BidAddresses;
using Tanafos.Main.Services.DTO.ReviewedSystemRequestLog;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nafis.Services.DTO.Notification;
using Nafis.Services.DTO.Provider;
using Nafes.CrossCutting.Common.Security;
using Tanafos.Main.Services.DTO;
using Nafes.CrossCutting.Common.DTO;
using Nafis.Services.DTO.BuyTenderDocsPill;
using Nafis.Services.DTO.Company;
using Nafes.CrossCutting.Common.ReviewedSystemRequestLog;
using Nafis.Services.DTO.AssociationWithdraw;

namespace Tanafos.Main.Services.Implementation.Services
{
   
    public class BidService : IBidService
    {
        private readonly IBidCreationService _bidCreationService;
        private readonly IBidManagementService _bidManagementService;
        private readonly IBidSearchService _bidSearchService;
        private readonly IBidPublishingService _bidPublishingService;
        private readonly IBidPaymentService _bidPaymentService;
        private readonly IBidNotificationService _bidNotificationService;
        private readonly IBidStatisticsService _bidStatisticsService;

        public BidService(
            IBidCreationService bidCreationService,
            IBidManagementService bidManagementService,
            IBidSearchService bidSearchService,
            IBidPublishingService bidPublishingService,
            IBidPaymentService bidPaymentService,
            IBidNotificationService bidNotificationService,
            IBidStatisticsService bidStatisticsService)
        {
            _bidCreationService = bidCreationService;
            _bidManagementService = bidManagementService;
            _bidSearchService = bidSearchService;
            _bidPublishingService = bidPublishingService;
            _bidPaymentService = bidPaymentService;
            _bidNotificationService = bidNotificationService;
            _bidStatisticsService = bidStatisticsService;
        }

        #region Bid Creation Methods (delegated to IBidCreationService)

        public async Task<OperationResult<AddBidResponse>> AddBidNew(AddBidModelNew model)
            => await _bidCreationService.AddBidNew(model);

        public async Task<OperationResult<AddBidResponse>> AddInstantBid(AddInstantBid addInstantBidRequest)
            => await _bidCreationService.AddInstantBid(addInstantBidRequest);

        public async Task<OperationResult<long>> AddBidAddressesTimes(AddBidAddressesTimesModel model)
            => await _bidCreationService.AddBidAddressesTimes(model);

        public async Task<OperationResult<List<QuantitiesTable>>> AddBidQuantitiesTable(AddQuantitiesTableRequest model)
            => await _bidCreationService.AddBidQuantitiesTable(model);

        public async Task<OperationResult<AddBidAttachmentsResponse>> AddBidAttachments(AddBidAttachmentRequest model)
            => await _bidCreationService.AddBidAttachments(model);

        public async Task<OperationResult<AddInstantBidAttachmentResponse>> AddInstantBidAttachments(AddInstantBidsAttachments addInstantBidsAttachmentsRequest)
            => await _bidCreationService.AddInstantBidAttachments(addInstantBidsAttachmentsRequest);

        public async Task<OperationResult<List<UploadFileResponse>>> UploadBidAttachments(IFormCollection formCollection)
            => await _bidCreationService.UploadBidAttachments(formCollection);

        public async Task<OperationResult<List<UploadFileResponse>>> UploadBidAttachmentsNewsFile(IFormCollection formCollection)
            => await _bidCreationService.UploadBidAttachmentsNewsFile(formCollection);

        public async Task<OperationResult<long>> AddBidClassificationAreaAndExecution(AddBidClassificationAreaAndExecutionModel model)
            => await _bidCreationService.AddBidClassificationAreaAndExecution(model);

        public async Task<OperationResult<long>> AddBidNews(AddBidNewsModel model)
            => await _bidCreationService.AddBidNews(model);

        public async Task<OperationResult<long>> TenderExtend(AddBidAddressesTimesTenderExtendModel model)
            => await _bidCreationService.TenderExtend(model);

        public async Task<OperationResult<bool>> CopyBid(CopyBidRequest model)
            => await _bidCreationService.CopyBid(model);

        public async Task<OperationResult<bool>> DeleteDraftBid(long bidId)
            => await _bidCreationService.DeleteDraftBid(bidId);

        #endregion

        #region Bid Management Methods (delegated to IBidManagementService)

        public async Task<OperationResult<ReadOnlyBidModel>> GetBidDetails(long id)
            => await _bidManagementService.GetBidDetails(id);

        public async Task<OperationResult<ReadOnlyBidMainDataModel>> GetBidMainData(long id)
            => await _bidManagementService.GetBidMainData(id);

        public async Task<OperationResult<ReadOnlyBidResponse>> GetDetailsForBidByIdAsync(long bidId)
            => await _bidManagementService.GetDetailsForBidByIdAsync(bidId);

        public async Task<OperationResult<ReadOnlyPublicBidModel>> GetPublicBidDetails(long id)
            => await _bidManagementService.GetPublicBidDetails(id);

        public async Task<OperationResult<GetBidDetailsForShare>> GetBidDetailsForShare(long bidId)
            => await _bidManagementService.GetBidDetailsForShare(bidId);

        public async Task<OperationResult<ReadOnlyBidAddressesTimeModel>> GetBidAddressesTime(long bidId)
            => await _bidManagementService.GetBidAddressesTime(bidId);

        public async Task<OperationResult<ReadOnlyBidAttachmentRequest>> GetBidAttachment(long bidId)
            => await _bidManagementService.GetBidAttachment(bidId);

        public async Task<OperationResult<IReadOnlyList<ReadOnlyBidNewsModel>>> GetBidNews(long bidId)
            => await _bidManagementService.GetBidNews(bidId);

        public async Task<OperationResult<List<ReadOnlyQuantitiesTableModel>>> GetBidQuantitiesTable(long bidId)
            => await _bidManagementService.GetBidQuantitiesTable(bidId);

        public async Task<PagedResponse<List<ReadOnlyQuantitiesTableModel>>> GetBidQuantitiesTableNew(long bidId, int pageSize = 5, int pageNumber = 1)
            => await _bidManagementService.GetBidQuantitiesTableNew(bidId, pageSize, pageNumber);

        public async Task<(OperationResult<byte[]>, string)> GetZipFileForBidAttachmentAsBinary(long bidId)
            => await _bidManagementService.GetZipFileForBidAttachmentAsBinary(bidId);

        public async Task<OperationResult<ReadOnlyBidStatusDetailsModel>> GetBidStatusDetails(long bidId)
            => await _bidManagementService.GetBidStatusDetails(bidId);

        public async Task<OperationResult<List<BidStatusResponse>>> GetBidStatusWithDates(long bidId)
            => await _bidManagementService.GetBidStatusWithDates(bidId);

        public async Task<OperationResult<List<BidStatusResponse>>> GetBidStatusWithDatesForBid(Bid bidInDb)
            => await _bidManagementService.GetBidStatusWithDatesForBid(bidInDb);

        public async Task<OperationResult<List<BidStatusResponse>>> GetBidStatusWithDatesForInstantBids(Bid bidInDb)
            => await _bidManagementService.GetBidStatusWithDatesForInstantBids(bidInDb);

        public async Task<List<BidStatusResponse>> OrderTimelineByIndexIfIgnoreTimelineIsTrue(List<BidStatusResponse> model)
            => await _bidManagementService.OrderTimelineByIndexIfIgnoreTimelineIsTrue(model);

        public async Task<OperationResult<bool>> UpdateReadProviderRead(long id)
            => await _bidManagementService.UpdateReadProviderRead(id);

        public async Task<OperationResult<bool>> IsBidInEvaluation(long bidId)
            => await _bidManagementService.IsBidInEvaluation(bidId);

        public async Task<OperationResult<bool>> ToggelsAbleToSubscribeToBid(long bidId)
            => await _bidManagementService.ToggelsAbleToSubscribeToBid(bidId);

        public async Task<OperationResult<bool>> RevealBidInCaseNotSubscribe(long bidId)
            => await _bidManagementService.RevealBidInCaseNotSubscribe(bidId);

        public async Task<OperationResult<long>> AddRFIandRequests(AddRFIRequestModel model)
            => await _bidManagementService.AddRFIandRequests(model);

        public async Task<OperationResult<IReadOnlyList<ReadOnlyBidRFiRequestModel>>> GetBidRFiRequests(long bidId, int typeId)
            => await _bidManagementService.GetBidRFiRequests(bidId, typeId);

        public async Task<OperationResult<GetUserRoleResponse>> GetUserRole()
            => await _bidManagementService.GetUserRole();

        public async Task<OperationResult<int>> GetStoppingPeriod()
            => await _bidManagementService.GetStoppingPeriod();

        public async Task<OperationResult<QuantityStableSettings>> QuantityStableSettings()
            => await _bidManagementService.QuantityStableSettings();

        public async Task<OperationResult<GetEntityContactDetailsResponse>> GetEntityContactDetails(GetEntityContactDetailsRequest request)
            => await _bidManagementService.GetEntityContactDetails(request);

        public async Task<string> GetBidCreatorName(Bid bid)
            => await _bidManagementService.GetBidCreatorName(bid);

        public async Task<string> GetBidCreatorEmailToReceiveEmails(Bid bid)
            => await _bidManagementService.GetBidCreatorEmailToReceiveEmails(bid);

        public async Task<(string, string)> GetBidCreatorImage(Bid bid)
            => await _bidManagementService.GetBidCreatorImage(bid);

        #endregion

        #region Bid Search Methods (delegated to IBidSearchService)

        public async Task<PagedResponse<IReadOnlyList<ReadOnlyFilterBidModel>>> GetBidsList(FilterBidsSearchModel request)
            => await _bidSearchService.GetBidsList(request);

        public async Task<PagedResponse<IReadOnlyList<ReadOnlyFilterBidModel>>> GetBidsCreatedByUser(GetBidsCreatedByUserModel request)
            => await _bidSearchService.GetBidsCreatedByUser(request);

        public async Task<PagedResponse<IReadOnlyList<ReadOnlyFilterBidModel>>> GetAssociationBids(GetBidsCreatedByUserModel request)
            => await _bidSearchService.GetAssociationBids(request);

        public async Task<PagedResponse<IReadOnlyList<ReadOnlyPublicBidListModel>>> GetPublicBidsList(int pageSize, int pageNumber)
            => await _bidSearchService.GetPublicBidsList(pageSize, pageNumber);

        public async Task<PagedResponse<IReadOnlyList<ReadOnlyPublicBidListModel>>> GetPublicFreelancingBidsList(FilterBidsSearchModel request)
            => await _bidSearchService.GetPublicFreelancingBidsList(request);

        public async Task<PagedResponse<List<GetMyBidResponse>>> GetMyBidsAsync(FilterBidsSearchModel model)
            => await _bidSearchService.GetMyBidsAsync(model);

        public async Task<PagedResponse<IReadOnlyList<GetProvidersBidsReadOnly>>> GetpProviderBids(int pageSize = 10, int pageNumber = 1)
            => await _bidSearchService.GetpProviderBids(pageSize, pageNumber);

        public async Task<PagedResponse<IReadOnlyList<GetMyBidResponse>>> GetAllBids(FilterBidsSearchModel request)
            => await _bidSearchService.GetAllBids(request);

        public async Task<OperationResult<GetBidsSearchHeadersResponse>> GetBidsSearchHeadersAsync()
            => await _bidSearchService.GetBidsSearchHeadersAsync();

        public async Task<PagedResponse<IReadOnlyList<GetProvidersBidsReadOnly>>> GetAssociationProviderBids(int pageSize = 10, int pageNumber = 1)
            => await _bidSearchService.GetAssociationProviderBids(pageSize, pageNumber);

        #endregion

        #region Bid Publishing Methods (delegated to IBidPublishingService)

        public async Task<OperationResult<bool>> TakeActionOnPublishingBidByAdmin(PublishBidDto request)
            => await _bidPublishingService.TakeActionOnPublishingBidByAdmin(request);

        public async Task ExecutePostPublishingLogic(Bid bid, ApplicationUser usr, TenderStatus oldStatusOfBid)
            => await _bidPublishingService.ExecutePostPublishingLogic(bid, usr, oldStatusOfBid);

        public async Task<OperationResult<bool>> TakeActionOnBidByDonor(long bidDonorId, DonorResponse donorResponse)
            => await _bidPublishingService.TakeActionOnBidByDonor(bidDonorId, donorResponse);

        public async Task<OperationResult<bool>> TakeActionOnBidSubmissionBySupervisingBid(BidSupervisingActionRequest req)
            => await _bidPublishingService.TakeActionOnBidSubmissionBySupervisingBid(req);

        public async Task SendEmailAndNotifyDonor(Bid bid)
            => await _bidPublishingService.SendEmailAndNotifyDonor(bid);

        public async Task SendUpdatedBidEmailToCreatorAndProvidersOfThisBid(Bid bid)
            => await _bidPublishingService.SendUpdatedBidEmailToCreatorAndProvidersOfThisBid(bid);

        #endregion

        #region Bid Payment Methods (delegated to IBidPaymentService)

        public async Task<OperationResult<ReadOnlyGetBidPriceModel>> GetBidPrice(GetBidDocumentsPriceRequestModel request)
            => await _bidPaymentService.GetBidPrice(request);

        public async Task<OperationResult<ReadOnlyGetBidPriceModel>> GetBidPriceForFreelancer(GetBidDocumentsPriceRequestModel request)
            => await _bidPaymentService.GetBidPriceForFreelancer(request);

        public async Task<OperationResult<BuyTermsBookResponseModel>> BuyTermsBook(BuyTermsBookModel model)
            => await _bidPaymentService.BuyTermsBook(model);

        public async Task<OperationResult<BuyTenderDocsPillModel>> GetBuyTenderDocsPillModel(long providerBidId)
            => await _bidPaymentService.GetBuyTenderDocsPillModel(providerBidId);

        public async Task<OperationResult<GetProviderDataOfRefundableCompanyBidModel>> GetProviderDataOfRefundableCompanyBid(long companyBidId)
            => await _bidPaymentService.GetProviderDataOfRefundableCompanyBid(companyBidId);

        public async Task<OperationResult<List<GetCompaniesToBuyTermsBookResponse>>> GetCurrentUserCompaniesToBuyTermsBookWithForbiddenReasonsIfFoundAsync(long bidId, long? currenctUserSpecificCompanyId = null)
            => await _bidPaymentService.GetCurrentUserCompaniesToBuyTermsBookWithForbiddenReasonsIfFoundAsync(bidId, currenctUserSpecificCompanyId);

        public async Task<OperationResult<GetFreelancersToBuyTermsBookResponse>> GetCurrentUserFreelancersToBuyTermsBookWithForbiddenReasonsIfFoundAsync(long bidId, long? freelancerId)
            => await _bidPaymentService.GetCurrentUserFreelancersToBuyTermsBookWithForbiddenReasonsIfFoundAsync(bidId, freelancerId);

        #endregion

        #region Bid Notification Methods (delegated to IBidNotificationService)

        public async Task<OperationResult<bool>> InviteProvidersWithSameCommercialSectors(long bidId, bool isAutomatically = false)
            => await _bidNotificationService.InviteProvidersWithSameCommercialSectors(bidId, isAutomatically);

        public async Task<OperationResult<List<InvitedCompanyResponseDto>>> GetAllInvitedCompaniesForBidAsync(GetAllInvitedCompaniesRequestModel request)
            => await _bidNotificationService.GetAllInvitedCompaniesForBidAsync(request);

        public async Task<OperationResult<List<GetReviewedSystemRequestLogResponse>>> GetProviderInvitationLogs(long bidId)
            => await _bidNotificationService.GetProviderInvitationLogs(bidId);

        public async Task<(List<NotificationReceiverUser> ActualReceivers, List<NotificationReceiverUser> RealtimeReceivers)> GetProvidersUserIdsWhoBoughtTermsPolicyForNotification(Bid bid)
            => await _bidNotificationService.GetProvidersUserIdsWhoBoughtTermsPolicyForNotification(bid);

        #endregion

        #region Bid Statistics Methods (delegated to IBidStatisticsService)

        public async Task<OperationResult<long>> IncreaseBidViewCount(long bidId)
            => await _bidStatisticsService.IncreaseBidViewCount(bidId);

        public async Task<PagedResponse<List<GetBidViewsModel>>> GetBidViews(long bidId, int pageSize, int pageNumber)
            => await _bidStatisticsService.GetBidViews(bidId, pageSize, pageNumber);

        public async Task<OperationResult<BidViewsStatisticsResponse>> GetBidViewsStatisticsAsync(long bidId)
            => await _bidStatisticsService.GetBidViewsStatisticsAsync(bidId);

        public long GetBidCreatorId(Bid bid)
            => _bidStatisticsService.GetBidCreatorId(bid);

        public async Task<List<ProviderBid>> GetProviderBidsWithAssociationFees(List<ProvidersBidsWithdrawModel> providerBidsIds, long creatorId, UserType creatorType)
            => await _bidStatisticsService.GetProviderBidsWithAssociationFees(providerBidsIds, creatorId, creatorType);

        public (decimal, decimal) GetTanafosAssociationFeesOfBoughtTermsBooks(IEnumerable<ProviderBid> pbs)
            => _bidStatisticsService.GetTanafosAssociationFeesOfBoughtTermsBooks(pbs);

        public (decimal, decimal) GetTanafosAssociationFeesOfBoughtTermsBook(ProviderBid pb)
           => _bidStatisticsService.GetTanafosAssociationFeesOfBoughtTermsBook(pb);



        #endregion
    }
}
