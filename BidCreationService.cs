using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nafes.Base.Model;
using Nafes.CrossCutting.Common;
using Nafes.CrossCutting.Common.API;
using Nafes.CrossCutting.Common.BackgroundTask;
using Nafes.CrossCutting.Common.Cache;
using Nafes.CrossCutting.Common.DTO;
using Nafes.CrossCutting.Common.Helpers;
using Nafes.CrossCutting.Common.Interfaces;
using Nafes.CrossCutting.Common.OperationResponse;
using Nafes.CrossCutting.Common.ReviewedSystemRequestLog;
using Nafes.CrossCutting.Common.Security;
using Nafes.CrossCutting.Common.Sendinblue;
using Nafes.CrossCutting.Common.Settings;
using Nafes.CrossCutting.Data.Repository;
using Nafes.CrossCutting.Model.Entities;
using Nafes.CrossCutting.Model.Enums;
using Nafes.CrossCutting.Model.Lookups;
using Nafis.Services.Contracts;
using Nafis.Services.Contracts.CommonServices;
using Nafis.Services.Contracts.Factories;
using Nafis.Services.Contracts.Repositories;
using Nafis.Services.DTO.Association;
using Nafis.Services.DTO.Bid;
using Nafis.Services.DTO.BidAnnouncement;
using Nafis.Services.DTO.BuyTenderDocsPill;
using Nafis.Services.DTO.CommonServices;
using Nafis.Services.DTO.Notification;
using Nafis.Services.Extensions;
using Nafis.Services.Hubs;
using Nafis.Services.Implementation.CommonServices.NotificationHelper;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Tanafos.Main.Services.Contracts;
using Tanafos.Main.Services.Contracts.CommonServices;
using Tanafos.Main.Services.DTO.Bid;
using Tanafos.Main.Services.DTO.BidAddresses;
using Tanafos.Main.Services.DTO.Emails.Bids;
using Tanafos.Main.Services.DTO.Point;
using Tanafos.Main.Services.Implementation.Services;
using Tanafos.Shared.Service.Contracts.CommonServices;
using Tanafos.Shared.Service.DTO.CommonServices;
using static Nafes.CrossCutting.Model.Enums.BidEventsEnum;
using static Nafis.Services.DTO.Bid.AddBidModel;

namespace Nafis.Services.Implementation
{
    public class BidCreationService : IBidCreationService
    {
        private readonly BidServiceCore _bidServiceCore;
        private readonly ICrossCuttingRepository<Bid, long> _bidRepository;
        private readonly ICrossCuttingRepository<RFP, long> _rfpRepository;
        private readonly ICrossCuttingRepository<Donor, long> _donorRepository;
        private readonly ICrossCuttingRepository<BidRegion, int> _bidRegionsRepository;
        private readonly ICrossCuttingRepository<Nafes.CrossCutting.Model.Lookups.Region, int> _regionRepository;
        private readonly ICrossCuttingRepository<TenderSubmitQuotation, long> _tenderSubmitQuotationRepository;
        private readonly ICrossCuttingRepository<UserFavBidList, long> _userFavBidList;
        private readonly ILoggerService<BidService> _logger;
        private readonly IMapper _mapper;
        private readonly IHelperService _helperService;
        private readonly ICurrentUserService _currentUserService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICrossCuttingRepository<Association, long> _associationRepository;
        private readonly ICrossCuttingRepository<BidAddressesTime, long> _bidAddressesTimeRepository;
        private readonly ICrossCuttingRepository<QuantitiesTable, long> _bidQuantitiesTableRepository;
        private readonly ICrossCuttingRepository<BidAttachment, long> _bidAttachmentRepository;
        private readonly ICrossCuttingRepository<BidNews, long> _bidNewsRepository;
        private readonly ICrossCuttingRepository<BidAddressesTimeLog, long> _bidAddressesTimeLogRepository;
        private readonly ICrossCuttingRepository<ProviderBidExtension, long> _providerBidExtensionRepository;
        private readonly FileSettings fileSettings;
        private readonly IImageService _imageService;
        private readonly ICrossCuttingRepository<Association_Additional_Contact_Detail, int> _associationAdditional_ContactRepository;
        private readonly GeneralSettings _generalSettings;
        private readonly ICrossCuttingRepository<ProviderBid, long> _providerBidRepository;
        private readonly ICrossCuttingRepository<Provider, long> _providerRepository;
        private readonly ICrossCuttingRepository<BidInvitations, long> _bidInvitationsRepository;
        private readonly ICompanyService _companyService;
        private readonly ICrossCuttingRepository<Company, long> _companyRepository;
        private readonly ICrossCuttingRepository<AwardingSelect, long> _awardingSelectRepository;
        private readonly IEmailService _emailService;
        private readonly IAppGeneralSettingService _appGeneralSettingService;
        private readonly ICrossCuttingRepository<BidMainClassificationMapping, long> _bidMainClassificationMappingRepository;
        private readonly IDateTimeZone _dateTimeZone;
        private readonly ICrossCuttingRepository<Bid_Industry, long> _bidIndustryRepository;
        private readonly ICrossCuttingRepository<Company_Industry, long> _companyIndustryRepository;
        private readonly ICrossCuttingRepository<Notification, long> _notificationRepository;
        private readonly ICrossCuttingRepository<NotificationRecivers, long> _notificationReciversRepository;
        private readonly IHubContext<NotificationHub> _notificationHubContext;
        private readonly ICompressService _compressService;
        private readonly ICrossCuttingRepository<InvitationRequiredDocument, long> _invitationRequiredDocumentRepository;
        private readonly ITenderSubmitQuotationRepositoryAsync _bidsOfProviderRepository;
        private readonly ICrossCuttingRepository<Industry, long> _industryRepository;
        private readonly ICrossCuttingRepository<AwardingProvider, long> _awardingProviderRepository;
        private readonly ICrossCuttingRepository<ProviderQuantitiesTableDetails, long> _providerQuantitiesTableDetailsRepository;
        private readonly ICrossCuttingRepository<DemoSettings, long> _demoSettingsRepository;
        private readonly ICrossCuttingRepository<Contract, long> _contractRepository;
        private readonly IAssociationService _associationService;
        private readonly ICrossCuttingRepository<AppGeneralSetting, long> _appGeneralSettingsRepository;
        private readonly ICompanyUserRolesService _companyUserRolesService;
        private readonly ICrossCuttingRepository<Provider_Additional_Contact_Detail, long> _providerAdditionalContactDetailRepository;
        private readonly ICrossCuttingRepository<BidViewsLog, long> _bidViewsLogRepository;
        private readonly ICrossCuttingRepository<Organization, long> _organizatioRepository;
        private readonly ICrossCuttingRepository<PayTabTransaction, long> _payTabTransactionRepository;
        private readonly IEncryption _encryptionService;
        private readonly ICrossCuttingRepository<BidTypesBudgets, long> _bidTypesBudgetsRepository;
        private readonly IDemoSettingsService _demoSettingsService;
        private readonly ICacheStoreService _cacheStoreService;
        private readonly ICrossCuttingRepository<CommercialSectorsTree, long> _CommercialSectorsTreeRepository;
        private readonly ICrossCuttingRepository<InvitedAssociationsByDonor, long> _invitedAssociationsByDonorRepository;
        private readonly ICrossCuttingRepository<BidDonor, long> _BidDonorRepository;
        private readonly IDonorService _donorService;
        private readonly ICrossCuttingRepository<BidType, int> _bidTypeRepository;
        private readonly ISendinblueService _sendinblueService;
        private readonly SendinblueOptions _sendinblueOptions;
        private readonly INotifyInBackgroundService _notifyInBackgroundService;
        private readonly ICrossCuttingRepository<ProviderRefundTransaction, long> _providerRefundTransactionRepository;
        private readonly ICrossCuttingRepository<BidSupervisingData, long> _bidSupervisingDataRepository;
        private readonly INotificationUserClaim _notificationUserClaim;
        private readonly ICrossCuttingRepository<Coupon, long> _couponRepository;
        private readonly ICrossCuttingRepository<CouponUsagesHistory, long> _couponUsageHistoryRepository;
        private readonly ICouponServiceCommonMethodsForPayments _bidAndCouponServicesCommonMethods;
        private readonly ICrossCuttingRepository<BidAchievementPhases, long> _bidAchievementPhasesRepository;
        private readonly ICrossCuttingRepository<CancelBidRequest, long> _cancelBidRequestRepository;
        private readonly ICrossCuttingRepository<Nafes.CrossCutting.Model.Lookups.TermsBookPrice, int> _termsBookPriceRepository;
        private readonly IUserSearchService _userSearchService;
        private readonly IReviewedSystemRequestLogService _reviewedSystemRequestLogService;
        private readonly IConvertViewService _convertViewService;
        private readonly IEmailSettingService _emailSettingService;
        private readonly ISMSService _sMSService;
        private readonly IPointEventService _pointEventService;
        private readonly IBidAnnouncementService _bidAnnouncementService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IUploadingFiles _uploadingFiles;
        private readonly ICrossCuttingRepository<FinancialDemand, long> _financialRequestRepository;
        private readonly IBackgroundQueue _backgroundQueue;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ICrossCuttingRepository<BIdWithHtml, long> _bIdWithHtmlRepository;
        private readonly IInvoiceService _invoiceService;
        private readonly ICrossCuttingRepository<OrganizationUser, long> _organizationUserRepository;
        private readonly IBidSearchLogService _bidSearchLogService;
        private readonly ICrossCuttingRepository<ManualCompany, long> _manualCompanyRepository;
        private readonly ICrossCuttingRepository<SubscriptionPayment, long> _subscriptionPaymentRepository;
        private readonly ISubscriptionAddonsService _subscriptionAddonsService;
        private readonly IChannelWriter<GenerateTenderDocsPillModel> _channelWriterTenderDocs;
        private readonly ICommonEmailAndNotificationService _commonEmailAndNotificationService;
        private readonly ICrossCuttingRepository<FreelanceBidIndustry, long> _freelanceBidIndustryRepository;
        private readonly ICrossCuttingRepository<Freelancer, long> _freelancerRepository;
        private readonly ICrossCuttingRepository<FreelancerFreelanceWorkingSector, long> _freelancerFreelanceWorkingSectorRepository;
        private readonly ICrossCuttingRepository<SubscriptionPaymentFeature, long> _subscriptionPaymentFeatureRepository;
        private readonly ICrossCuttingRepository<SubscriptionPaymentFeatureUsage, long> _subscriptionPaymentFeatureUsageRepository;
        private readonly ICrossCuttingRepository<BidRevealLog, long> _bidRevealLogRepository;

       
        public BidCreationService(
            BidServiceCore bidServiceCore,
            ICrossCuttingRepository<Bid, long> bidRepository,
            ICrossCuttingRepository<RFP, long> rfpRepository,
            ICrossCuttingRepository<Donor, long> donorRepository,
            ICrossCuttingRepository<BidRegion, int> bidRegionsRepository,
            ICrossCuttingRepository<Nafes.CrossCutting.Model.Lookups.Region, int> regionRepository,
            ICrossCuttingRepository<TenderSubmitQuotation, long> tenderSubmitQuotationRepository,
            ICrossCuttingRepository<UserFavBidList, long> userFavBidList,
            ILoggerService<BidService> logger,
            IMapper mapper,
            IHelperService helperService,
            ICurrentUserService currentUserService,
            UserManager<ApplicationUser> userManager,
            ICrossCuttingRepository<Association, long> associationRepository,
            ICrossCuttingRepository<BidAddressesTime, long> bidAddressesTimeRepository,
            ICrossCuttingRepository<QuantitiesTable, long> bidQuantitiesTableRepository,
            ICrossCuttingRepository<BidAttachment, long> bidAttachmentRepository,
            ICrossCuttingRepository<BidNews, long> bidNewsRepository,
            ICrossCuttingRepository<BidAddressesTimeLog, long> bidAddressesTimeLogRepository,
            ICrossCuttingRepository<ProviderBidExtension, long> providerBidExtensionRepository,
            IOptions<FileSettings> fileSettings,
            IImageService imageService,
            ICrossCuttingRepository<Association_Additional_Contact_Detail, int> associationAdditionalContactRepository,
            IOptions<GeneralSettings> generalSettings,
            ICrossCuttingRepository<ProviderBid, long> providerBidRepository,
            ICrossCuttingRepository<Provider, long> providerRepository,
            ICrossCuttingRepository<BidInvitations, long> bidInvitationsRepository,
            ICompanyService companyService,
            ICrossCuttingRepository<Company, long> companyRepository,
            ICrossCuttingRepository<AwardingSelect, long> awardingSelectRepository,
            IEmailService emailService,
            IAppGeneralSettingService appGeneralSettingService,
            ICrossCuttingRepository<BidMainClassificationMapping, long> bidMainClassificationMappingRepository,
            IDateTimeZone dateTimeZone,
            ICrossCuttingRepository<Bid_Industry, long> bidIndustryRepository,
            ICrossCuttingRepository<Company_Industry, long> companyIndustryRepository,
            ICrossCuttingRepository<Notification, long> notificationRepository,
            ICrossCuttingRepository<NotificationRecivers, long> notificationReciversRepository,
            IHubContext<NotificationHub> notificationHubContext,
            ICompressService compressService,
            ICrossCuttingRepository<InvitationRequiredDocument, long> invitationRequiredDocumentRepository,
            ITenderSubmitQuotationRepositoryAsync bidsOfProviderRepository,
            ICrossCuttingRepository<Industry, long> industryRepository,
            ICrossCuttingRepository<AwardingProvider, long> awardingProviderRepository,
            ICrossCuttingRepository<ProviderQuantitiesTableDetails, long> providerQuantitiesTableDetailsRepository,
            ICrossCuttingRepository<DemoSettings, long> demoSettingsRepository,
            ICrossCuttingRepository<Contract, long> contractRepository,
            IAssociationService associationService,
            ICrossCuttingRepository<AppGeneralSetting, long> appGeneralSettingsRepository,
            ICompanyUserRolesService companyUserRolesService,
            ICrossCuttingRepository<Provider_Additional_Contact_Detail, long> providerAdditionalContactDetailRepository,
            ICrossCuttingRepository<BidViewsLog, long> bidViewsLogRepository,
            ICrossCuttingRepository<Organization, long> organizatioRepository,
            ICrossCuttingRepository<PayTabTransaction, long> payTabTransactionRepository,
            IEncryption encryptionService,
            ICrossCuttingRepository<BidTypesBudgets, long> bidTypesBudgetsRepository,
            IDemoSettingsService demoSettingsService,
            ICacheStoreService cacheStoreService,
            ICrossCuttingRepository<CommercialSectorsTree, long> commercialSectorsTreeRepository,
            ICrossCuttingRepository<InvitedAssociationsByDonor, long> invitedAssociationsByDonorRepository,
            ICrossCuttingRepository<BidDonor, long> bidDonorRepository,
            IDonorService donorService,
            ICrossCuttingRepository<BidType, int> bidTypeRepository,
            ISendinblueService sendinblueService,
            IOptions<SendinblueOptions> sendinblueOptions,
            INotifyInBackgroundService notifyInBackgroundService,
            ICrossCuttingRepository<ProviderRefundTransaction, long> providerRefundTransactionRepository,
            ICrossCuttingRepository<BidSupervisingData, long> bidSupervisingDataRepository,
            INotificationUserClaim notificationUserClaim,
            ICrossCuttingRepository<Coupon, long> couponRepository,
            ICrossCuttingRepository<CouponUsagesHistory, long> couponUsageHistoryRepository,
            ICouponServiceCommonMethodsForPayments bidAndCouponServicesCommonMethods,
            ICrossCuttingRepository<BidAchievementPhases, long> bidAchievementPhasesRepository,
            ICrossCuttingRepository<CancelBidRequest, long> cancelBidRequestRepository,
            ICrossCuttingRepository<Nafes.CrossCutting.Model.Lookups.TermsBookPrice, int> termsBookPriceRepository,
            IUserSearchService userSearchService,
            IReviewedSystemRequestLogService reviewedSystemRequestLogService,
            IConvertViewService convertViewService,
            IEmailSettingService emailSettingService,
            ISMSService sMSService,
            IPointEventService pointEventService,
            IBidAnnouncementService bidAnnouncementService,
            IServiceProvider serviceProvider,
            IUploadingFiles uploadingFiles,
            ICrossCuttingRepository<FinancialDemand, long> financialRequestRepository,
            IBackgroundQueue backgroundQueue,
            IServiceScopeFactory serviceScopeFactory,
            ICrossCuttingRepository<BIdWithHtml, long> bIdWithHtmlRepository,
            IInvoiceService invoiceService,
            ICrossCuttingRepository<OrganizationUser, long> organizationUserRepository,
            IBidSearchLogService bidSearchLogService,
            ICrossCuttingRepository<ManualCompany, long> manualCompanyRepository,
            ICrossCuttingRepository<SubscriptionPayment, long> subscriptionPaymentRepository,
            ISubscriptionAddonsService subscriptionAddonsService,
            IChannelWriter<GenerateTenderDocsPillModel> channelWriterTenderDocs,
            ICommonEmailAndNotificationService commonEmailAndNotificationService,
            ICrossCuttingRepository<FreelanceBidIndustry, long> freelanceBidIndustryRepository,
            ICrossCuttingRepository<Freelancer, long> freelancerRepository,
            ICrossCuttingRepository<FreelancerFreelanceWorkingSector, long> freelancerFreelanceWorkingSectorRepository,
            ICrossCuttingRepository<SubscriptionPaymentFeature, long> subscriptionPaymentFeatureRepository,
            ICrossCuttingRepository<SubscriptionPaymentFeatureUsage, long> subscriptionPaymentFeatureUsageRepository,
            ICrossCuttingRepository<BidRevealLog, long> bidRevealLogRepository)
        {
            _bidServiceCore = bidServiceCore;
            _bidRepository = bidRepository;
            _rfpRepository = rfpRepository;
            _donorRepository = donorRepository;
            _bidRegionsRepository = bidRegionsRepository;
            _regionRepository = regionRepository;
            _tenderSubmitQuotationRepository = tenderSubmitQuotationRepository;
            _userFavBidList = userFavBidList;
            _logger = logger;
            _mapper = mapper;
            _helperService = helperService;
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _userManager = userManager;
            _associationRepository = associationRepository;
            _bidAddressesTimeRepository = bidAddressesTimeRepository;
            _bidQuantitiesTableRepository = bidQuantitiesTableRepository;
            _bidAttachmentRepository = bidAttachmentRepository;
            _bidNewsRepository = bidNewsRepository;
            _bidAddressesTimeLogRepository = bidAddressesTimeLogRepository;
            _providerBidExtensionRepository = providerBidExtensionRepository;
            this.fileSettings = fileSettings.Value;
            this._imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
            _associationAdditional_ContactRepository = associationAdditionalContactRepository;
            _generalSettings = generalSettings.Value;
            _providerBidRepository = providerBidRepository;
            _providerRepository = providerRepository;
            _bidInvitationsRepository = bidInvitationsRepository;
            _companyService = companyService;
            _companyRepository = companyRepository;
            _awardingSelectRepository = awardingSelectRepository;
            this._emailService = emailService;
            _appGeneralSettingService = appGeneralSettingService;
            _bidMainClassificationMappingRepository = bidMainClassificationMappingRepository;
            _dateTimeZone = dateTimeZone;
            _bidIndustryRepository = bidIndustryRepository;
            _companyIndustryRepository = companyIndustryRepository;
            _notificationRepository = notificationRepository;
            _notificationReciversRepository = notificationReciversRepository;
            _notificationHubContext = notificationHubContext;
            this._compressService = compressService;
            _invitationRequiredDocumentRepository = invitationRequiredDocumentRepository;
            _bidsOfProviderRepository = bidsOfProviderRepository;
            _industryRepository = industryRepository;
            _awardingProviderRepository = awardingProviderRepository;
            _providerQuantitiesTableDetailsRepository = providerQuantitiesTableDetailsRepository;
            _demoSettingsRepository = demoSettingsRepository;
            _contractRepository = contractRepository;
            _associationService = associationService;
            _appGeneralSettingsRepository = appGeneralSettingsRepository;
            _companyUserRolesService = companyUserRolesService;
            _providerAdditionalContactDetailRepository = providerAdditionalContactDetailRepository;
            _bidViewsLogRepository = bidViewsLogRepository;
            _organizatioRepository = organizatioRepository;
            _payTabTransactionRepository = payTabTransactionRepository;
            _encryptionService = encryptionService;
            _bidTypesBudgetsRepository = bidTypesBudgetsRepository;
            _demoSettingsService = demoSettingsService;
            _cacheStoreService = cacheStoreService;
            _CommercialSectorsTreeRepository = commercialSectorsTreeRepository;
            _invitedAssociationsByDonorRepository = invitedAssociationsByDonorRepository;
            _BidDonorRepository = bidDonorRepository;
            _donorService = donorService;
            _bidTypeRepository = bidTypeRepository;
            _sendinblueService = sendinblueService;
            _sendinblueOptions = sendinblueOptions.Value;
            this._notifyInBackgroundService = notifyInBackgroundService;
            _providerRefundTransactionRepository = providerRefundTransactionRepository;
            _bidSupervisingDataRepository = bidSupervisingDataRepository;
            _notificationUserClaim = notificationUserClaim;
            _couponRepository = couponRepository;
            _couponUsageHistoryRepository = couponUsageHistoryRepository;
            _bidAndCouponServicesCommonMethods = bidAndCouponServicesCommonMethods;
            _bidAchievementPhasesRepository = bidAchievementPhasesRepository;
            _cancelBidRequestRepository = cancelBidRequestRepository;
            _termsBookPriceRepository = termsBookPriceRepository;
            _userSearchService = userSearchService;
            _reviewedSystemRequestLogService = reviewedSystemRequestLogService;
            _convertViewService = convertViewService;
            _emailSettingService = emailSettingService;
            _sMSService = sMSService;
            _pointEventService = pointEventService;
            _bidAnnouncementService = bidAnnouncementService;
            _serviceProvider = serviceProvider;
            _uploadingFiles = uploadingFiles;
            _financialRequestRepository = financialRequestRepository;
            _backgroundQueue = backgroundQueue;
            _serviceScopeFactory = serviceScopeFactory;
            _bIdWithHtmlRepository = bIdWithHtmlRepository;
            _invoiceService = invoiceService;
            _organizationUserRepository = organizationUserRepository;
            _bidSearchLogService = bidSearchLogService;
            _manualCompanyRepository = manualCompanyRepository;
            _subscriptionPaymentRepository = subscriptionPaymentRepository;
            _subscriptionAddonsService = subscriptionAddonsService;
            _channelWriterTenderDocs = channelWriterTenderDocs;
            _commonEmailAndNotificationService = commonEmailAndNotificationService;
            _freelanceBidIndustryRepository = freelanceBidIndustryRepository;
            _freelancerRepository = freelancerRepository;
            _freelancerFreelanceWorkingSectorRepository = freelancerFreelanceWorkingSectorRepository;
            _subscriptionPaymentFeatureRepository = subscriptionPaymentFeatureRepository;
            _subscriptionPaymentFeatureUsageRepository = subscriptionPaymentFeatureUsageRepository;
            _bidRevealLogRepository = bidRevealLogRepository;
        }
        //public async Task<OperationResult<AddBidResponse>> AddBidNew(AddBidModelNew model)
        //    => await _bidServiceCore.AddBidNew(model);

        public async Task<OperationResult<AddBidResponse>> AddBidNew(AddBidModelNew model)
        {
            try
            {
                var usr = _currentUserService.CurrentUser;
                if (usr is null)
                    return OperationResult<AddBidResponse>.Fail(HttpErrorCode.NotAuthenticated);
                if (_currentUserService.IsUserNotAuthorized(new List<UserType> { UserType.Association, UserType.Donor, UserType.SuperAdmin, UserType.Admin }))
                    return OperationResult<AddBidResponse>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);

                if ((usr.UserType == UserType.SuperAdmin || usr.UserType == UserType.Admin) && model.Id == 0)
                    return OperationResult<AddBidResponse>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);

                var adjustBidAddressesToTheEndOfDayResult = _bidServiceCore.AdjustRequestBidAddressesToTheEndOfTheDay(model);
                if (!adjustBidAddressesToTheEndOfDayResult.IsSucceeded)
                    return OperationResult<AddBidResponse>.Fail(adjustBidAddressesToTheEndOfDayResult.HttpErrorCode, adjustBidAddressesToTheEndOfDayResult.Code);

                if (_bidServiceCore.IsRequiredDataForNotSaveAsDraftAdded(model))
                    return OperationResult<AddBidResponse>.Fail(HttpErrorCode.InvalidInput, CommonErrorCodes.INVALID_INPUT);


                var generalSettingsResult = await _appGeneralSettingService.GetAppGeneralSettings();
                if (!generalSettingsResult.IsSucceeded)
                    return OperationResult<AddBidResponse>.Fail(generalSettingsResult.HttpErrorCode, generalSettingsResult.Code);

                var generalSettings = generalSettingsResult.Data;

                long bidId = 0;
                var oldBidName = model.BidName;

                Association association = null;
                if (usr.UserType == UserType.Association)
                {
                    association = await _associationService.GetUserAssociation(usr.Email);
                    if (association == null)
                        return OperationResult<AddBidResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.ASSOCIATION_NOT_FOUND);
                }

                Donor donor = null;
                if (usr.UserType == UserType.Donor)
                {
                    donor = await _bidServiceCore.GetDonorUser(usr);
                    if (donor == null)
                        return OperationResult<AddBidResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.DONOR_NOT_FOUND);
                }


                _bidServiceCore.ValidateBidFinancialValueWithBidType(model);

                if (model.Id != 0)
                {
                    if (_bidServiceCore.ValidateBidInvitationAttachmentsNew(model))
                    {
                        return OperationResult<AddBidResponse>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.ADDING_INVITATION_ATTACHMENTS_REQUIRED);
                    }

                    var bid = await _bidRepository.FindOneAsync(x => x.Id == model.Id, false, nameof(Bid.Bid_Industries)
                        , nameof(Bid.Association), nameof(Bid.BidAddressesTime), nameof(Bid.BidSupervisingData));
                    if (bid == null)
                        return OperationResult<AddBidResponse>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.INVALID_BID);

                    var validationOfBidDates = _bidServiceCore. ValidateBidDates(model, bid, generalSettings);
                    if (!validationOfBidDates.IsSucceeded)
                        return OperationResult<AddBidResponse>.Fail(validationOfBidDates.HttpErrorCode, validationOfBidDates.Code, validationOfBidDates.ErrorMessage);

                    if ((usr.UserType != UserType.SuperAdmin && usr.UserType != UserType.Admin)
                        && (bid.EntityId != usr.CurrentOrgnizationId || bid.EntityType != usr.UserType))
                        return OperationResult<AddBidResponse>.Fail(HttpErrorCode.NotAuthorized, CommonErrorCodes.NotAuthorized);


                    if (usr.UserType == UserType.SuperAdmin || usr.UserType != UserType.Admin || usr.UserType == UserType.Donor)
                    {
                        var res = await _bidServiceCore.AddInvitationToAssocationByDonorIfFound(model.InvitedAssociationByDonor, bid, model.IsAssociationFoundToSupervise, model.SupervisingAssociationId);
                        if (!res.IsSucceeded)
                            return OperationResult<AddBidResponse>.Fail(res.HttpErrorCode, res.Code);
                    }
                    if ((usr.UserType == UserType.SuperAdmin || usr.UserType == UserType.Admin) && bid.BidStatusId != (int)TenderStatus.Open && bid.BidStatusId != (int)TenderStatus.Draft && bid.BidStatusId != (int)TenderStatus.Reviewing)
                        return OperationResult<AddBidResponse>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);

                    //if (bid.BidStatusId != (int)TenderStatus.Draft)
                    //        return OperationResult<AddBidResponse>.Fail(HttpErrorCode.NotAuthorized, "you can edit bid when it is draft or rejected only.");
                    if (bid.BidStatusId != (int)TenderStatus.Rejected && bid.BidStatusId != (int)TenderStatus.Draft && (usr.UserType == UserType.Association || usr.UserType == UserType.Donor))
                        return OperationResult<AddBidResponse>.Fail(HttpErrorCode.NotAuthorized, BidErrorCodes.YOU_CAN_EDIT_BID_WHEN_IT_IS_DRAFT_OR_REJECTED_ONLY);

                    _bidServiceCore.UpdateSiteMapLastModificationDateIfSpecificDataChanged(bid, model);
                    bidId = bid.Id;
                    bid.BidName = model.BidName;
                    bid.Objective = model.Objective;
                    if (await _bidServiceCore.CheckIfWeCanUpdatePriceOfBid(usr, bid))
                    {
                        var calculationResult = _bidServiceCore.CalculateAndUpdateBidPrices(model.Association_Fees, generalSettings, bid);
                        if (!calculationResult.IsSucceeded)
                            return OperationResult<AddBidResponse>.Fail(calculationResult.HttpErrorCode, calculationResult.Code, calculationResult.ErrorMessage);
                    }

                    bid.BidOffersSubmissionTypeId = model.BidOffersSubmissionTypeId == 0 ? null : model.BidOffersSubmissionTypeId;
                    bid.IsFunded = model.IsFunded;
                    bid.FunderName = model.FunderName;
                    bid.IsBidAssignedForAssociationsOnly = model.IsBidAssignedForAssociationsOnly;
                    bid.BidDonorId = !model.IsFunded ? null : bid.BidDonorId;

                    if (bid.BidStatusId == (int)TenderStatus.Draft)
                    {
                        bid.CreatedBy = usr.Id;
                        bid.CreationDate = _dateTimeZone.CurrentDate;
                    }
                    else
                    {
                        bid.ModifiedBy = usr.Id;
                        bid.ModificationDate = _dateTimeZone.CurrentDate;
                    }
                    bid.IsInvitationNeedAttachments = model.IsInvitationNeedAttachments.HasValue ? model.IsInvitationNeedAttachments.Value : false;

                    bid.IsFinancialInsuranceRequired = model.IsFinancialInsuranceRequired;
                    bid.FinancialInsuranceValue = model.BidFinancialInsuranceValue;

                    await _bidRepository.Update(bid);

                    await _bidServiceCore.ValidateInvitationAttachmentsAndUpdateThemNew(model, usr);

                    await _bidServiceCore.UpdateBidRegions(model.RegionsId, bidId);

                    #region add Bid Commerical Sectors
                    List<Bid_Industry> bidIndustries = new List<Bid_Industry>();
                    var bidIndustryLST = (await _bidIndustryRepository.FindAsync(x => x.BidId == bid.Id, false)).ToList();

                    var parentIgnoredCommercialSectorIds = await _helperService.DeleteParentSectorsIdsFormList(model.IndustriesIds);

                    foreach (var cid in parentIgnoredCommercialSectorIds)
                    {
                        var bidIndustry = new Bid_Industry();
                        bidIndustry.BidId = bid.Id;
                        bidIndustry.CommercialSectorsTreeId = cid;
                        bidIndustry.CreatedBy = usr.Id;
                        bidIndustries.Add(bidIndustry);
                    }
                    bid.Bid_Industries = bidIndustries;
                    await _bidIndustryRepository.DeleteRangeAsync(bidIndustryLST);
                    await _bidIndustryRepository.AddRange(bidIndustries);
                    #endregion

                    #region Bid Address

                    var bidAddressesTime = await _bidAddressesTimeRepository.FindOneAsync(x => x.BidId == model.Id, false);
                    if (bidAddressesTime != null)
                    {
                        var bidAddressesTimesId = bidAddressesTime.Id;
                        bidAddressesTime.BidId = model.Id;
                        if (bid.BidStatusId == (int)TenderStatus.Open && (usr.UserType == UserType.SuperAdmin || usr.UserType == UserType.Admin))
                        {

                            bidAddressesTime.LastDateInReceivingEnquiries = bidAddressesTime.LastDateInReceivingEnquiries < _dateTimeZone.CurrentDate ?
                                bidAddressesTime.LastDateInReceivingEnquiries : model.LastDateInReceivingEnquiries;
                            bidAddressesTime.LastDateInOffersSubmission = bidAddressesTime.LastDateInOffersSubmission < _dateTimeZone.CurrentDate ?
                                bidAddressesTime.LastDateInOffersSubmission : model.LastDateInOffersSubmission;
                            bidAddressesTime.OffersOpeningDate = bidAddressesTime.OffersOpeningDate < _dateTimeZone.CurrentDate ?
                                bidAddressesTime.OffersOpeningDate : model.OffersOpeningDate.Value.Date;

                            if (model.OffersOpeningDate != null && model.OffersOpeningDate != default)
                                bidAddressesTime.ExpectedAnchoringDate = bidAddressesTime.ExpectedAnchoringDate < _dateTimeZone.CurrentDate ?
                                bidAddressesTime.ExpectedAnchoringDate : (model.ExpectedAnchoringDate != null && model.ExpectedAnchoringDate != default)
                                ? model.ExpectedAnchoringDate.Value.Date
                                : model.OffersOpeningDate is null ? model.OffersOpeningDate : model.OffersOpeningDate.Value.AddBusinessDays(generalSettings.StoppingPeriodDays + 1).Date;

                        }
                        else
                        {
                            bidAddressesTime.LastDateInReceivingEnquiries = model.LastDateInReceivingEnquiries;
                            bidAddressesTime.LastDateInOffersSubmission = model.LastDateInOffersSubmission;
                            bidAddressesTime.OffersOpeningDate = model.OffersOpeningDate is null ? model.OffersOpeningDate : model.OffersOpeningDate.Value.Date;

                            bidAddressesTime.ExpectedAnchoringDate = (model.ExpectedAnchoringDate != null && model.ExpectedAnchoringDate != default)
                                ? model.ExpectedAnchoringDate.Value.Date
                                : model.OffersOpeningDate is null ? model.OffersOpeningDate : model.OffersOpeningDate.Value.AddBusinessDays(generalSettings.StoppingPeriodDays + 1).Date;

                        }

                        await _bidAddressesTimeRepository.Update(bidAddressesTime);
                        if (bid.BidStatusId == (int)TenderStatus.Open && (usr.UserType == UserType.SuperAdmin || usr.UserType == UserType.Admin))
                            await _bidServiceCore.UpdateBidStatus(bid.Id);

                    }
                    else
                    {
                        if (bid.BidStatusId == (int)TenderStatus.Draft)
                        {
                            var entityBidAddressesTime = new BidAddressesTime();
                            entityBidAddressesTime.StoppingPeriod = generalSettings.StoppingPeriodDays;
                            entityBidAddressesTime.OffersOpeningDate = model.OffersOpeningDate is null ? model.OffersOpeningDate : model.OffersOpeningDate.Value.Date;
                            entityBidAddressesTime.LastDateInOffersSubmission = model.LastDateInOffersSubmission;
                            entityBidAddressesTime.LastDateInReceivingEnquiries = model.LastDateInReceivingEnquiries;
                            entityBidAddressesTime.BidId = bid.Id;
                            entityBidAddressesTime.EnquiriesStartDate = bid.CreationDate;
                            if (model.OffersOpeningDate != null && model.OffersOpeningDate != default)
                                entityBidAddressesTime.ExpectedAnchoringDate = (model.ExpectedAnchoringDate != null && model.ExpectedAnchoringDate != default)
                                ? model.ExpectedAnchoringDate.Value.Date
                                : model.OffersOpeningDate is null ? model.OffersOpeningDate : model.OffersOpeningDate.Value.AddBusinessDays(generalSettings.StoppingPeriodDays + 1).Date;

                            if (model.OffersOpeningDate != null && model.LastDateInOffersSubmission != null && model.LastDateInReceivingEnquiries != null)
                                await _bidAddressesTimeRepository.Add(entityBidAddressesTime);
                        }
                    }
                    #endregion

                    if (model.IsFunded)
                    {
                        var res = await _bidServiceCore.SaveBidDonor(model.DonorRequest, bid.Id, usr.Id);
                        if (!res.IsSucceeded)
                            return OperationResult<AddBidResponse>.Fail(res.HttpErrorCode, res.Code);
                    }
                    else
                    {
                        var oldBidDonors = await _BidDonorRepository.FindAsync(x => x.BidId == bid.Id);
                        if (oldBidDonors.Any())
                            await _BidDonorRepository.DeleteRangeAsync(oldBidDonors.ToList());
                    }
                    if (model.BidName != oldBidName)
                        await _bidServiceCore.UpdateBidRelatedAttachmentsFileNameAfterBidNameChanging(bidId, model.BidName);

                    return OperationResult<AddBidResponse>.Success(new AddBidResponse { Id = bid.Id, Ref_Number = bid.Ref_Number, BidVisibility = (BidTypes)bid.BidTypeId });
                }
                else
                {

                    var validationOfBidDates = _bidServiceCore.ValidateBidDates(model, null, generalSettings);
                    if (!validationOfBidDates.IsSucceeded)
                        return OperationResult<AddBidResponse>.Fail(validationOfBidDates.HttpErrorCode, validationOfBidDates.Code, validationOfBidDates.ErrorMessage);

                    var entity = _mapper.Map<Bid>(model);
                    if (_bidServiceCore.ValidateBidInvitationAttachmentsNew(model))
                        return OperationResult<AddBidResponse>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.ADDING_INVITATION_ATTACHMENTS_REQUIRED);

                    var calculationResult = _bidServiceCore.CalculateAndUpdateBidPrices(model.Association_Fees, generalSettings, entity);
                    if (!calculationResult.IsSucceeded)
                        return OperationResult<AddBidResponse>.Fail(calculationResult.HttpErrorCode, calculationResult.Code, calculationResult.ErrorMessage);
                    //generate code
                    string firstPart_Ref_Number = _dateTimeZone.CurrentDate.ToString("yy") + _dateTimeZone.CurrentDate.ToString("MM") + model.BidTypeId.ToString();
                    string randomNumber = await _bidServiceCore.GenerateBidRefNumber(model.Id, firstPart_Ref_Number);

                    entity.SiteMapDataLastModificationDate = _dateTimeZone.CurrentDate;
                    entity.EntityId = usr.CurrentOrgnizationId;
                    entity.DonorId = donor?.Id;
                    entity.EntityType = usr.UserType;
                    entity.Ref_Number = randomNumber;
                    entity.IsDeleted = false;
                    entity.AssociationId = association?.Id;
                    entity.BidStatusId = (int)TenderStatus.Draft;
                    entity.CreatedBy = usr.Id;
                    entity.Objective = model.Objective;
                    entity.IsInvitationNeedAttachments = model.IsInvitationNeedAttachments.HasValue ? model.IsInvitationNeedAttachments.Value : false;
                    entity.IsBidAssignedForAssociationsOnly = model.IsBidAssignedForAssociationsOnly;

                    entity.BidTypeId = model.BidTypeId;
                    entity.BidVisibility = (BidTypes)entity.BidTypeId.Value;
                    entity.BidOffersSubmissionTypeId = model.BidOffersSubmissionTypeId == 0 ? null : model.BidOffersSubmissionTypeId;

                    await _bidRepository.Add(entity);
                    if (usr.UserType == UserType.Donor)
                    {
                        var res = await _bidServiceCore.AddInvitationToAssocationByDonorIfFound(model.InvitedAssociationByDonor, entity, model.IsAssociationFoundToSupervise, model.SupervisingAssociationId);
                        if (!res.IsSucceeded)
                        {
                            await this._bidRepository.Delete(entity);
                            return OperationResult<AddBidResponse>.Fail(res.HttpErrorCode, res.Code);
                        }
                    }

                    bidId = entity.Id;

                    _bidServiceCore.AddInvitationAttachmentsNew(model, usr, bidId);

                    await _bidServiceCore.AddBidRegions(model.RegionsId, bidId);

                    #region add Bid Commerical Sectors


                    List<Bid_Industry> bid_Industries = new List<Bid_Industry>();
                    var bid_IndustryLST = (await _bidIndustryRepository.FindAsync(x => x.BidId == bidId, false)).ToList();

                    var parentIgnoredCommercialSectorIds = await _helperService.DeleteParentSectorsIdsFormList(model.IndustriesIds);
                    foreach (var cid in parentIgnoredCommercialSectorIds)
                    {
                        var bid_Industry = new Bid_Industry();
                        bid_Industry.BidId = bidId;
                        bid_Industry.CommercialSectorsTreeId = cid;
                        bid_Industry.CreatedBy = usr.Id;
                        if (!(bid_IndustryLST.Where(a => a.CommercialSectorsTreeId == cid).Any()))
                            bid_Industries.Add(bid_Industry);
                    }
                    entity.Bid_Industries = bid_Industries;
                    await _bidIndustryRepository.AddRange(bid_Industries);
                    #endregion

                    #region Bid Address
                    var entityBidAddressesTime = new BidAddressesTime();
                    //_mapper.Map<BidAddressesTime>(model);
                    entityBidAddressesTime.StoppingPeriod = generalSettings.StoppingPeriodDays;
                    entityBidAddressesTime.OffersOpeningDate = model.OffersOpeningDate != null ? model.OffersOpeningDate.Value.Date : model.OffersOpeningDate;
                    entityBidAddressesTime.LastDateInOffersSubmission = model.LastDateInOffersSubmission;
                    entityBidAddressesTime.LastDateInReceivingEnquiries = model.LastDateInReceivingEnquiries;
                    //entityBidAddressesTime.InvitationDocumentsApplyingEndDate = model.InvitationDocumentsApplyingEndDate;
                    entityBidAddressesTime.BidId = entity.Id;
                    entityBidAddressesTime.EnquiriesStartDate = entity.CreationDate;
                    entityBidAddressesTime.ExpectedAnchoringDate = (model.ExpectedAnchoringDate != null && model.ExpectedAnchoringDate != default)
                        ? model.ExpectedAnchoringDate.Value.Date
                        : model.OffersOpeningDate != null ?
                        model.OffersOpeningDate.Value.AddBusinessDays(generalSettings.StoppingPeriodDays + 1).Date :
                        null;
                    await _bidServiceCore.UpdateInvitationRequiredDocumentsEndDateNew(model.InvitationDocumentsApplyingEndDate, entity);

                    await _bidAddressesTimeRepository.Add(entityBidAddressesTime);
                    //    bidAddressesTimesId = entity.Id;
                    #endregion

                    if (model.IsFunded)
                    {
                        var res = await _bidServiceCore.SaveBidDonor(model.DonorRequest, bidId, usr.Id);
                        if (!res.IsSucceeded)
                            return OperationResult<AddBidResponse>.Fail(res.HttpErrorCode, res.Code);
                    }

                    return OperationResult<AddBidResponse>.Success(new AddBidResponse
                    {
                        Id = bidId,
                        Ref_Number = entity.Ref_Number,
                        BidVisibility = (BidTypes)entity.BidTypeId
                    });
                }
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = model,
                    ErrorMessage = "Failed to Add Bid!",
                    ControllerAndAction = "BidController/AddBidNew"
                });
                return OperationResult<AddBidResponse>.Fail(
                       HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);

            }
        }

        //public async Task<OperationResult<AddBidResponse>> AddInstantBid(AddInstantBid addInstantBidRequest)
        //    => await _bidServiceCore.AddInstantBid(addInstantBidRequest);

        public async Task<OperationResult<AddBidResponse>> AddInstantBid(AddInstantBid addInstantBidRequest)
        {
            try
            {
                var usr = _currentUserService.CurrentUser;

                var authorizedTypes = new List<UserType>() { UserType.Association, UserType.Donor, UserType.SuperAdmin, UserType.Admin };
                if (usr == null || !authorizedTypes.Contains(usr.UserType))
                    return OperationResult<AddBidResponse>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);

                if (addInstantBidRequest.BidType != BidTypes.Instant && addInstantBidRequest.BidType != BidTypes.Freelancing)
                    return OperationResult<AddBidResponse>.Fail(HttpErrorCode.InvalidInput, CommonErrorCodes.INVALID_INPUT);


                if (!addInstantBidRequest.IsDraft && _bidServiceCore.validateAddInstantBidRequest(addInstantBidRequest, out var requiredParams))
                    return OperationResult<AddBidResponse>.Fail(HttpErrorCode.InvalidInput, CommonErrorCodes.INVALID_INPUT, requiredParams);

                if (!addInstantBidRequest.IsDraft && addInstantBidRequest.BidType == BidTypes.Instant && addInstantBidRequest.RegionsId.IsNullOrEmpty())
                    return OperationResult<AddBidResponse>.Fail(HttpErrorCode.InvalidInput, CommonErrorCodes.INVALID_INPUT);


                var bidTypeBudget = await _bidTypesBudgetsRepository.FindOneAsync(x => x.Id == addInstantBidRequest.BidTypeBudgetId, false, nameof(BidTypesBudgets.BidType));
                if (bidTypeBudget is null && !addInstantBidRequest.IsDraft)
                    return OperationResult<AddBidResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.NOT_FOUND);

                if ((usr.UserType == UserType.SuperAdmin || usr.UserType == UserType.Admin) && addInstantBidRequest.Id == 0)
                    return OperationResult<AddBidResponse>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);

                Association association;
                if (usr.UserType == UserType.Association)
                {
                    association = await _associationService.GetUserAssociation(usr.Email);
                    if (association == null)
                        return OperationResult<AddBidResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.ASSOCIATION_NOT_FOUND);
                }

                Donor donor;
                if (usr.UserType == UserType.Donor)
                {
                    donor = await _donorRepository.FindOneAsync(don => don.Id == usr.CurrentOrgnizationId && don.isVerfied && !don.IsDeleted);
                    if (donor == null)
                        return OperationResult<AddBidResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.DONOR_NOT_FOUND);
                }

                if (addInstantBidRequest.Id != 0)
                    return await _bidServiceCore.EditInstantBid(addInstantBidRequest, usr, bidTypeBudget);

                return await _bidServiceCore.AddInstantBid(addInstantBidRequest, usr, bidTypeBudget);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = addInstantBidRequest,
                    ErrorMessage = "Failed to add instant bid !",
                    ControllerAndAction = "BidController/AddInstantBid"
                });
                return OperationResult<AddBidResponse>.Fail(
                       HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }


        }

        //public async Task<OperationResult<long>> AddBidAddressesTimes(AddBidAddressesTimesModel model)
        //    => await _bidServiceCore.AddBidAddressesTimes(model);
        public async Task<OperationResult<long>> AddBidAddressesTimes(AddBidAddressesTimesModel model)
        {
            var usr = _currentUserService.CurrentUser;

            var authorizedTypes = new List<UserType>() { UserType.Association, UserType.Donor };
            if (usr == null || !authorizedTypes.Contains(usr.UserType))
                return OperationResult<long>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);

            try
            {
                long bidAddressesTimesId = 0;
                var bid = await _bidRepository.FindOneAsync(x => x.Id == model.BidId, false);

                if (bid == null)
                {
                    return OperationResult<long>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.INVALID_BID);
                }
                Association association = null;
                if (usr.UserType == UserType.Association)
                {
                    association = await _associationService.GetUserAssociation(usr.Email);
                    if (association == null)
                        return OperationResult<long>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.INVALID_ASSOCIATION);
                }
                Donor donor = null;
                if (usr.UserType == UserType.Donor)
                {
                    donor = await _donorRepository.FindOneAsync(don => don.Id == usr.CurrentOrgnizationId && don.isVerfied && !don.IsDeleted);
                    if (donor == null)
                        return OperationResult<long>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.DONOR_NOT_FOUND);
                }


                //if (usr.Email.ToLower() == association.Manager_Email.ToLower())
                //    return OperationResult<long>.Fail(HttpErrorCode.NotAuthorized, "As a manager you have not an authority to add or edit bid.");
                //if (usr.Email.ToLower() != association.Email.ToLower() && _associationAdditional_ContactRepository.FindOneAsync(a => a.Email.ToLower() == usr.Email.ToLower()) == null)
                //    return OperationResult<long>.Fail(HttpErrorCode.NotAuthorized, "You must be a creator to add or edit bid.");

                var generalSettingsResult = await _appGeneralSettingService.GetAppGeneralSettings();
                if (!generalSettingsResult.IsSucceeded)
                    return OperationResult<long>.Fail(generalSettingsResult.HttpErrorCode, generalSettingsResult.Code);

                var generalSettings = generalSettingsResult.Data;

                if (model.LastDateInReceivingEnquiries < _dateTimeZone.CurrentDate.Date)
                    return OperationResult<long>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.LAST_DATE_IN_RECEIVING_ENQUIRIES_MUST_NOT_BE_BEFORE_TODAY_DATE);
                if (model.LastDateInReceivingEnquiries > model.LastDateInOffersSubmission)
                    return OperationResult<long>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.LAST_DATE_IN_OFFERS_SUBMISSION_MUST_BE_GREATER_THAN_LAST_DATE_IN_RECEIVING_ENQUIRIES);
                if (model.OffersOpeningDate < model.LastDateInOffersSubmission)
                    return OperationResult<long>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.OFFERS_OPENING_DATE_MUST_BE_GREATER_THAN_LAST_DATE_IN_OFFERS_SUBMISSION);
                ////if (model.OffersInvestigationDate < model.OffersOpeningDate)
                ////return OperationResult<long>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.OFFERS_INVESTIGATION_DATE_MUST_BE_GREATER_THAN_OFFERS_OPENING_DATE);
                if (model.ExpectedAnchoringDate != null && model.ExpectedAnchoringDate != default
                   && model.ExpectedAnchoringDate < model.OffersOpeningDate.AddDays(generalSettings.StoppingPeriodDays + 1))
                    return OperationResult<long>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.EXPECTED_ANCHORING_DATE_MUST_BE_GREATER_THAN_OFFERS_OPENING_DATE_PLUS_STOPPING_PERIOD);
                ////if (model.WorkStartDate != null && model.WorkStartDate != default && model.ExpectedAnchoringDate != null && model.ExpectedAnchoringDate != default && model.WorkStartDate < model.OffersInvestigationDate && model.WorkStartDate < model.ExpectedAnchoringDate)
                ////    return OperationResult<long>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.WORK_START_DATE_MUST_NOT_BE_BEFORE_THE_DATE_SELECTED_FOR_OFFERS_INVESTIGATION_AND_ALSO_NOT_BEFORE_THE_DATE_SELECTED_AS_EXPECTED_ANCHORING_DATE_IF_ADDED);

                //if (model.EnquiriesStartDate > model.LastDateInReceivingEnquiries && model.EnquiriesStartDate < _dateTimeZone.CurrentDate.Date)
                //return OperationResult<long>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.ENQUIRIES_START_DATE_MUST_NOT_BE_BEFORE_TODAY_DATE_AND_NOT_AFTER_THE_DATE_SELECTED_AS_LAST_DATE_IN_RECEIVING_ENQUIRIES);

                var bidAddressesTime = await _bidAddressesTimeRepository.FindOneAsync(x => x.BidId == model.BidId, false);
                //edit
                if (bidAddressesTime != null)
                {
                    //var bidAddressesTime = _bidAddressesTimeRepository.FindOneAsync(x => x.Id == model.Id, false);

                    //if (bidAddressesTime == null)
                    //{
                    //    return OperationResult<long>.Fail(HttpErrorCode.InvalidInput, "Invalid bid Addresses Time");
                    //}
                    //if (usr.Id != bid.CreatedBy)
                    //    return OperationResult<long>.Fail(HttpErrorCode.NotAuthorized, "to edit bid You must be the person who creates it.");
                    bidAddressesTimesId = bidAddressesTime.Id;
                    bidAddressesTime.BidId = model.BidId;
                    //bidAddressesTime.OffersOpeningPlace = model.OffersOpeningPlace;
                    bidAddressesTime.LastDateInReceivingEnquiries = model.LastDateInReceivingEnquiries;
                    bidAddressesTime.LastDateInOffersSubmission = model.LastDateInOffersSubmission;
                    bidAddressesTime.OffersOpeningDate = model.OffersOpeningDate.Date;
                    //bidAddressesTime.OffersInvestigationDate = model.OffersInvestigationDate;
                    //bidAddressesTime.StoppingPeriod = model.StoppingPeriod;
                    bidAddressesTime.ExpectedAnchoringDate = (model.ExpectedAnchoringDate != null && model.ExpectedAnchoringDate != default)
                        ? model.ExpectedAnchoringDate.Value.Date
                        : model.OffersOpeningDate.AddDays(generalSettings.StoppingPeriodDays + 1).Date;
                    //bidAddressesTime.WorkStartDate = model.WorkStartDate;
                    //bidAddressesTime.ConfirmationLetterDueDate = model.ConfirmationLetterDueDate;
                    //bidAddressesTime.EnquiriesStartDate = model.EnquiriesStartDate;
                    //bidAddressesTime.MaximumPeriodForAnswering = model.MaximumPeriodForAnswering;

                    await _bidAddressesTimeRepository.Update(bidAddressesTime);
                }
                else
                {
                    var entity = _mapper.Map<BidAddressesTime>(model);
                    entity.StoppingPeriod = generalSettings.StoppingPeriodDays;
                    entity.EnquiriesStartDate = bid.CreationDate;
                    entity.ExpectedAnchoringDate = (model.ExpectedAnchoringDate != null && model.ExpectedAnchoringDate != default)
                        ? model.ExpectedAnchoringDate.Value
                        : model.OffersOpeningDate.AddDays(generalSettings.StoppingPeriodDays + 1);
                    await _bidServiceCore.UpdateInvitationRequiredDocumentsEndDate(model, bid);

                    await _bidAddressesTimeRepository.Add(entity);
                    bidAddressesTimesId = entity.Id;
                }

                return OperationResult<long>.Success(model.BidId);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = model,
                    ErrorMessage = "Failed to Add Bid Addresses Times!",
                    ControllerAndAction = "BidController/AddBidAddressesTimes"
                });
                return OperationResult<long>.Fail(
                       HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }


        //public async Task<OperationResult<List<QuantitiesTable>>> AddBidQuantitiesTable(AddQuantitiesTableRequest model)
        //    => await _bidServiceCore.AddBidQuantitiesTable(model);
        public async Task<OperationResult<List<QuantitiesTable>>> AddBidQuantitiesTable(AddQuantitiesTableRequest model)
        {

            try
            {
                var usr = _currentUserService.CurrentUser;
                var authorizedTypes = new List<UserType>() { UserType.Association, UserType.Donor, UserType.SuperAdmin, UserType.Admin };
                if (usr == null || !authorizedTypes.Contains(usr.UserType))
                    return OperationResult<List<QuantitiesTable>>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);
                var bid = await _bidRepository.FindOneAsync(x => x.Id == model.BidId, false, nameof(Bid.Association));

                if (bid == null)
                    return OperationResult<List<QuantitiesTable>>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.INVALID_BID);

                if (bid.EntityType != usr.UserType && (usr.UserType != UserType.SuperAdmin && usr.UserType != UserType.Admin))
                    return OperationResult<List<QuantitiesTable>>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);


                Association association = null;
                if (usr.UserType == UserType.Association)
                {
                    association = await _associationService.GetUserAssociation(usr.Email);
                    if (association == null)
                        return OperationResult<List<QuantitiesTable>>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.INVALID_ASSOCIATION);
                }
                else
                {
                    association = bid.Association;
                }

                Donor donor = null;
                if (usr.UserType == UserType.Donor)
                {
                    donor = await _bidServiceCore.GetDonorUser(usr);
                    if (donor == null)
                        return OperationResult<List<QuantitiesTable>>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.DONOR_NOT_FOUND);
                }

                //var existingQuantitiesTable_ContactList = await _bidQuantitiesTableRepository.Find(x => x.BidId == model.BidId).ToListAsync();
                //Delete Quantities Table
                //await _bidQuantitiesTableRepository.DeleteRangeAsync(existingQuantitiesTable_ContactList);

                var newQuantitiesTable = model.LstQuantitiesTable.Where(a => a.Id == 0).ToList();
                var EditQuantitiesTable = model.LstQuantitiesTable.Where(a => a.Id > 0).ToList();
                //Add  Quantities Table 
                var res = await _bidQuantitiesTableRepository.AddRange(newQuantitiesTable.Select(x =>
                {
                    var newEntity = _mapper.Map<QuantitiesTable>(x);
                    newEntity.BidId = model.BidId;
                    //newEntity.TotalPrice = x.ItemPrice * x.Quantity + ((x.ItemPrice * x.Quantity) * x.VATPercentage);
                    return newEntity;
                }).ToList());

                //edit Quantities table
                var existingQuantitiesTable = await _bidQuantitiesTableRepository.Find(x => x.BidId == model.BidId).ToListAsync();

                var deletedQuantityTables = new List<QuantitiesTable>();
                bool isQuantitiesChanged = false;

                foreach (var item in existingQuantitiesTable)
                {
                    var updatedquantity = EditQuantitiesTable.FirstOrDefault(x => x.Id == item.Id);
                    var newQuantity = res.FirstOrDefault(x => x.Id == item.Id);
                    if (newQuantity is not null)
                        continue;
                    if (updatedquantity is null && newQuantity is null)
                    {
                        deletedQuantityTables.Add(item);
                        continue;
                    }

                    if (updatedquantity.Quantity != item.Quantity && !isQuantitiesChanged)
                        isQuantitiesChanged = true;

                    item.ItemName = updatedquantity.ItemName;
                    item.ItemDesc = updatedquantity.ItemDesc;
                    item.Quantity = updatedquantity.Quantity;
                    item.Unit = updatedquantity.Unit;

                    await _bidQuantitiesTableRepository.Update(item);
                }
                await _bidQuantitiesTableRepository.DeleteRangeFromDBAsync(deletedQuantityTables);
                //Withrow all offers in case quantities is changed or adding or deleting row
                if (isQuantitiesChanged || newQuantitiesTable.Count > 0 || existingQuantitiesTable.Count != model.LstQuantitiesTable.Count)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var _tenderSubmitQuotationService = scope.ServiceProvider.GetRequiredService<ITenderSubmitQuotationService>();

                    var tenderSubmitQuotationCount = await _tenderSubmitQuotationRepository
                   .Find(a => a.BidId == model.BidId && a.ProposalStatus == ProposalStatus.Delivered)
                   .CountAsync();

                    //cancel all offers
                    var result = await _tenderSubmitQuotationService.CancelAllTenderSubmitQuotation(model.BidId);
                    //send announcement
                    if (tenderSubmitQuotationCount > 0)
                    {
                        var resAnnouncement = await _bidAnnouncementService.AddBidAnnouncementAfterEditQuantities(new AddBidAnnoucement
                        {
                            BidId = model.BidId,
                            Text = "(????? ???) ???? ??????? ??? ??? ?? ?? ????? ??? ????????? ?? ???? ???????? ???? ???? ????? ????? ?????? ???? ??? ??? ???????"
                        });
                    }
                }
                return OperationResult<List<QuantitiesTable>>.Success(res);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = model,
                    ErrorMessage = "Failed to Add Bid Quantities Table!",
                    ControllerAndAction = "BidController/AddBidQuantitiesTable"
                });
                return OperationResult<List<QuantitiesTable>>.Fail(
                       HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }


        //public async Task<OperationResult<AddBidAttachmentsResponse>> AddBidAttachments(AddBidAttachmentRequest model)
        //    => await _bidServiceCore.AddBidAttachments(model);
        public async Task<OperationResult<AddBidAttachmentsResponse>> AddBidAttachments(AddBidAttachmentRequest model)
        {
            try
            {
                var usr = _currentUserService.CurrentUser;
                var authorizedTypes = new List<UserType>() { UserType.Association, UserType.Donor, UserType.SuperAdmin, UserType.Admin };
                if (usr == null || !authorizedTypes.Contains(usr.UserType))
                    return OperationResult<AddBidAttachmentsResponse>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);
                if (model.Tender_Brochure_Policies_Url is null)
                    return OperationResult<AddBidAttachmentsResponse>.Fail(HttpErrorCode.InvalidInput, CommonErrorCodes.BID_NOT_FOUND);
                var bid = await _bidRepository
                    .Find(x => !x.IsDeleted && x.Id == model.BidId)
                    .Include(a => a.BidSupervisingData)
                    .IncludeBasicBidData()
                    .Include(x => x.BidRegions.Take(1))
                    .Include(x => x.QuantitiesTable)
                    .Include(x => x.BidAchievementPhases)
                    .ThenInclude(x => x.BidAchievementPhaseAttachments.Take(1))
                    .FirstOrDefaultAsync();

                if (bid is null)
                    return OperationResult<AddBidAttachmentsResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);

                if (bid.EntityType != usr.UserType && (usr.UserType != UserType.SuperAdmin && usr.UserType != UserType.Admin))
                    return OperationResult<AddBidAttachmentsResponse>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);

                if (usr.UserType == UserType.Association)
                {
                    var association = await _associationService.GetUserAssociation(usr.Email);
                    if (association == null)
                        return OperationResult<AddBidAttachmentsResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.ASSOCIATION_NOT_FOUND);

                    if (bid.AssociationId != association.Id)
                        return OperationResult<AddBidAttachmentsResponse>.Fail(HttpErrorCode.InvalidInput, CommonErrorCodes.INVALID_INPUT);
                }
                //else
                //{
                //    association = bid.Association;

                if (usr.UserType == UserType.Donor)
                {
                    var donor = await _bidServiceCore.GetDonorUser(usr);
                    if (donor == null)
                        return OperationResult<AddBidAttachmentsResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.DONOR_NOT_FOUND);

                    if (bid.DonorId != donor.Id)
                        return OperationResult<AddBidAttachmentsResponse>.Fail(HttpErrorCode.InvalidInput, CommonErrorCodes.INVALID_INPUT);
                }

                var checkQuantitesTableForThisBid = await _bidQuantitiesTableRepository.Find(a => a.BidId == bid.Id).AnyAsync();
                if (!checkQuantitesTableForThisBid)
                    return OperationResult<AddBidAttachmentsResponse>.Fail(HttpErrorCode.Conflict, CommonErrorCodes.PLEASE_FILL_ALL_REQUIRED_DATA_IN_PREVIOUS_STEPS);

                var oldStatusOfBid = (TenderStatus)bid.BidStatusId;

                string imagePath = !string.IsNullOrEmpty(model.Tender_Brochure_Policies_Url) ? _encryptionService.Decrypt(model.Tender_Brochure_Policies_Url) : null;

                bid.Tender_Brochure_Policies_Url = imagePath;
                bid.Tender_Brochure_Policies_FileName = model.Tender_Brochure_Policies_FileName;
                bid.TenderBrochurePoliciesType = model.TenderBrochurePoliciesType;

                if (model.RFPId != null && model.RFPId > 0)
                {
                    var isRFPExists = await _rfpRepository.Find(x => true).AnyAsync(x => x.Id == model.RFPId);
                    if (!isRFPExists)
                        return OperationResult<AddBidAttachmentsResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.RFP_NOT_FOUND);

                    bid.RFPId = model.RFPId;
                }
                else
                {
                    bid.RFPId = null;
                }
                if (model.BidStatusId.HasValue && _bidServiceCore.CheckIfWasDraftAndChanged(model.BidStatusId.Value, oldStatusOfBid) && !bid.CanPublishBid())
                    return OperationResult<AddBidAttachmentsResponse>.Fail(HttpErrorCode.InvalidInput, CommonErrorCodes.PLEASE_FILL_ALL_REQUIRED_DATA_IN_PREVIOUS_STEPS);

                List<BidAttachment> bidAttachmentsToSave = await _bidServiceCore.SaveBidAttachments(model, bid);
                var supervisingDonorClaims = await _donorService.GetFundedDonorSupervisingServiceClaims(bid.Id);
                //if (CheckIfAdminCanPublishBid(usr, bid))
                //    await ApplyClosedBidsLogicIfAdminTryToPublish(model, usr, bid, oldStatusOfBid);
                if (bid.BidTypeId != (int)BidTypes.Private)
                    await _bidServiceCore.ApplyClosedBidsLogic(model, usr, bid, supervisingDonorClaims);

                else
                    await _bidRepository.Update(bid);
                if (!_bidServiceCore.CheckIfHasSupervisor(bid, supervisingDonorClaims) && _bidServiceCore.CheckIfWeShouldSendPublishBidRequestToAdmins(bid, oldStatusOfBid))
                    await _bidServiceCore.SendPublishBidRequestEmailAndNotification(usr, bid, oldStatusOfBid);
                foreach (var file in bidAttachmentsToSave)
                {
                    file.AttachedFileURL = await _encryptionService.EncryptAsync(file.AttachedFileURL);
                }

                //if (usr.UserType == UserType.SuperAdmin || usr.UserType == UserType.Admin)
                //{
                //    if(bid.BidTypeId != (int)BidTypes.Private && oldStatusOfBid == TenderStatus.Draft && model.BidStatusId == (int)TenderStatus.Open) //Add approval review to bid incase of attachments are added by admin and bid type is not private.
                //        await AddSystemReviewToBidByCurrentUser(bid.Id, SystemRequestStatuses.Accepted); 

                //    if (model.IsSendEmailsAndNotificationAboutUpdatesChecked)
                //        await SendUpdatedBidEmailToCreatorAndProvidersOfThisBid(bid);
                //}    
                if (usr.UserType == UserType.SuperAdmin || usr.UserType == UserType.Admin)
                {
                    if (bid.BidTypeId != (int)BidTypes.Private && oldStatusOfBid == TenderStatus.Draft && model.BidStatusId == (int)TenderStatus.Open)
                    {
                        await _bidServiceCore.AddSystemReviewToBidByCurrentUser(bid.Id, SystemRequestStatuses.Accepted);

                        await _bidServiceCore.ExecutePostPublishingLogic(bid, usr, oldStatusOfBid);
                    }

                    if (model.IsSendEmailsAndNotificationAboutUpdatesChecked)
                        await _bidServiceCore.SendUpdatedBidEmailToCreatorAndProvidersOfThisBid(bid);
                }

                return OperationResult<AddBidAttachmentsResponse>.Success(new AddBidAttachmentsResponse
                {
                    Attachments = bidAttachmentsToSave,
                    BidRefNumber = bid.Ref_Number
                });
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = model,
                    ErrorMessage = "Failed to Add Attachments!",
                    ControllerAndAction = "BidController/AddBidAttachments"
                });
                return OperationResult<AddBidAttachmentsResponse>.Fail(HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }


        //public async Task<OperationResult<AddInstantBidAttachmentResponse>> AddInstantBidAttachments(AddInstantBidsAttachments addInstantBidsAttachmentsRequest)
        //    => await _bidServiceCore.AddInstantBidAttachments(addInstantBidsAttachmentsRequest);
        public async Task<OperationResult<AddInstantBidAttachmentResponse>> AddInstantBidAttachments(AddInstantBidsAttachments addInstantBidsAttachmentsRequest)
        {
            try
            {
                var usr = _currentUserService.CurrentUser;
                var authorizedTypes = new List<UserType>() { UserType.Association, UserType.Donor, UserType.SuperAdmin, UserType.Admin };
                if (usr == null || !authorizedTypes.Contains(usr.UserType))
                    return OperationResult<AddInstantBidAttachmentResponse>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);

                var bid = await _bidRepository.Find(x => x.Id == addInstantBidsAttachmentsRequest.BidId)
                                              .IncludeBasicBidData()
                                              .Include(x => x.BidRegions.Take(1))
                                              .Include(x => x.QuantitiesTable)
                                              .Include(x => x.BidAchievementPhases)
                                              .ThenInclude(x => x.BidAchievementPhaseAttachments.Take(1))
                                              .FirstOrDefaultAsync();

                var oldStatusOfbid = (TenderStatus)bid.BidStatusId;

                if (_bidServiceCore.IsCurrentUserBidCreator(usr, bid))
                    return OperationResult<AddInstantBidAttachmentResponse>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);

                var ValidationResponse = await _bidServiceCore.ValidateAddBidAttachmentsRequest(addInstantBidsAttachmentsRequest, bid, usr);
                if (!ValidationResponse.IsSucceeded)
                    return ValidationResponse;

                List<BidAttachment> bidAttachmentsToSave = await _bidServiceCore.MapInstantBidAttachments(addInstantBidsAttachmentsRequest, bid);
                bid.BidStatusId = addInstantBidsAttachmentsRequest.BidStatusId != null && addInstantBidsAttachmentsRequest.BidStatusId > 0 ?
                    Convert.ToInt32(addInstantBidsAttachmentsRequest.BidStatusId) : (int)TenderStatus.Reviewing;//approved

                var bidDonor = await _donorService.GetBidDonorOfBidIfFound(bid.Id);
                var supervisingDonorClaims = await _donorService.GetFundedDonorSupervisingServiceClaims(bid.Id);


                bid.BidStatusId = _bidServiceCore.CheckIfWeShouldMakeBidAtReviewingStatus(addInstantBidsAttachmentsRequest, usr, oldStatusOfbid) ? (int)TenderStatus.Reviewing
                    : addInstantBidsAttachmentsRequest.BidStatusId;
                bid.BidStatusId = _bidServiceCore.CheckIfWasDraftAndChanged(addInstantBidsAttachmentsRequest.BidStatusId.Value, oldStatusOfbid)
                    && Constants.AdminstrationUserTypesWithoutSupport.Contains(usr.UserType) ? (int)TenderStatus.Open : bid.BidStatusId;

                if (_bidServiceCore.CheckIfWeCanPublishBid(bid, oldStatusOfbid, bidDonor, supervisingDonorClaims))
                {

                    bid.CreationDate = _dateTimeZone.CurrentDate;
                    await _bidServiceCore.DoBusinessAfterPublishingBid(bid, usr);

                    await _pointEventService.AddPointEventUsageHistoryAsync(new AddPointEventUsageHistoryModel
                    {
                        PointType = PointTypes.PublishNonDraftBid,
                        ActionId = bid.Id,
                        EntityId = bid.AssociationId.HasValue ? bid.AssociationId.Value : bid.DonorId.Value,
                        EntityUserType = bid.AssociationId.HasValue ? UserType.Association : UserType.Donor,
                    });

                    await _bidServiceCore.LogBidCreationEvent(bid);
                }
                else if (bid.IsFunded && addInstantBidsAttachmentsRequest.BidStatusId != (int)TenderStatus.Draft && supervisingDonorClaims.Data.Any(x => x.ClaimType == SupervisingServiceClaimCodes.clm_3057 && x.IsChecked))
                {
                    await _bidServiceCore.SendBidToSponsorDonorToBeConfirmed(usr, bid, bidDonor);
                }
                await _bidRepository.Update(bid);
                if (!_bidServiceCore.CheckIfHasSupervisor(bid, supervisingDonorClaims) && _bidServiceCore.CheckIfWeShouldSendPublishBidRequestToAdmins(bid, oldStatusOfbid))
                    await _bidServiceCore.SendPublishBidRequestEmailAndNotification(usr, bid, oldStatusOfbid);

                if (usr.UserType == UserType.SuperAdmin || usr.UserType == UserType.Admin)
                {
                    if (bid.BidTypeId != (int)BidTypes.Private && oldStatusOfbid == TenderStatus.Draft && addInstantBidsAttachmentsRequest.BidStatusId == (int)TenderStatus.Open)
                    {
                        await _bidServiceCore.AddSystemReviewToBidByCurrentUser(bid.Id, SystemRequestStatuses.Accepted);  //Add approval review to bid incase of attachments are added by admin and bid type is not private.

                        await _bidServiceCore.ExecutePostPublishingLogic(bid, usr, oldStatusOfbid);


                    }
                    if (addInstantBidsAttachmentsRequest.IsSendEmailsAndNotificationAboutUpdatesChecked)
                        await _bidServiceCore.SendUpdatedBidEmailToCreatorAndProvidersOfThisBid(bid);
                }


                return OperationResult<AddInstantBidAttachmentResponse>.Success(new AddInstantBidAttachmentResponse
                {
                    Attachments = _mapper.Map<List<InstantBidAttachmentResponse>>(bidAttachmentsToSave),
                    BidRefNumber = bid.Ref_Number
                });
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = addInstantBidsAttachmentsRequest,
                    ErrorMessage = "Failed to add instant bid attachments !",
                    ControllerAndAction = "BidController/AddInstantBidAttachments"
                });
                return OperationResult<AddInstantBidAttachmentResponse>.Fail(HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }


        //public async Task<OperationResult<List<UploadFileResponse>>> UploadBidAttachments(IFormCollection formCollection)
        //    => await _bidServiceCore.UploadBidAttachments(formCollection);
        public async Task<OperationResult<List<UploadFileResponse>>> UploadBidAttachments(IFormCollection formCollection)
        {
            var filePathLst = await _uploadingFiles.UploadAsync(formCollection, fileSettings.Bid_Attachments_FilePath, "BidAtt", fileSettings.SpecialFilesMaxSizeInMega);

            if (filePathLst.IsSucceeded)
                return OperationResult<List<UploadFileResponse>>.Success(filePathLst.Data);
            else
                return OperationResult<List<UploadFileResponse>>.Fail(filePathLst.HttpErrorCode, filePathLst.Code, filePathLst.ErrorMessage);
        }

        //public async Task<OperationResult<List<UploadFileResponse>>> UploadBidAttachmentsNewsFile(IFormCollection formCollection)
        //    => await _bidServiceCore.UploadBidAttachmentsNewsFile(formCollection);
        public async Task<OperationResult<List<UploadFileResponse>>> UploadBidAttachmentsNewsFile(IFormCollection formCollection)
        {
            var filePathLst = await _uploadingFiles.UploadAsync(formCollection, fileSettings.BidAttachmentsNewsFilePath, "Bidnews", fileSettings.MaxSizeInMega);

            if (filePathLst.IsSucceeded)
                return OperationResult<List<UploadFileResponse>>.Success(filePathLst.Data);
            else
                return OperationResult<List<UploadFileResponse>>.Fail(filePathLst.HttpErrorCode, filePathLst.Code, filePathLst.ErrorMessage);
        }

        //public async Task<OperationResult<long>> AddBidClassificationAreaAndExecution(AddBidClassificationAreaAndExecutionModel model)
        //    => await _bidServiceCore.AddBidClassificationAreaAndExecution(model);
        public async Task<OperationResult<long>> AddBidClassificationAreaAndExecution(AddBidClassificationAreaAndExecutionModel model)
        {
            var usr = _currentUserService.CurrentUser;
            if (usr == null && usr.UserType != UserType.Association)
            {
                return OperationResult<long>.Fail(HttpErrorCode.NotAuthorized, CommonErrorCodes.NotAuthorized);
            }

            try
            {
                var bid = await _bidRepository.FindOneAsync(x => x.Id == model.Id, false);

                if (bid == null)
                {
                    return OperationResult<long>.Fail(HttpErrorCode.InvalidInput, CommonErrorCodes.NOT_FOUND);
                }

                var association = await _associationService.GetUserAssociation(usr.Email);
                if (association == null)
                    return OperationResult<long>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.ASSOCIATION_NOT_FOUND);

                //if (usr.Email.ToLower() == association.Manager_Email.ToLower())
                //    return OperationResult<long>.Fail(HttpErrorCode.NotAuthorized, "As a manager you have not an authority to add or edit bid.");
                //if (usr.Email.ToLower() != association.Email.ToLower() && _associationAdditional_ContactRepository.FindOneAsync(a => a.Email.ToLower() == usr.Email.ToLower()) == null)
                //    return OperationResult<long>.Fail(HttpErrorCode.NotAuthorized, "You must be a creator to add or edit bid.");

                //if (usr.Id != bid.CreatedBy)
                //    return OperationResult<long>.Fail(HttpErrorCode.NotAuthorized, "to edit bid You must be the person who creates it.");

                bid.ExecutionSite = model.ExecutionSite;
                List<BidMainClassificationMapping> bidMainClassificationMappings = new List<BidMainClassificationMapping>();
                var mainClassificationMappingLST = (await _bidMainClassificationMappingRepository.FindAsync(x => x.BidId == bid.Id, false)).ToList();

                foreach (var cid in model.BidMainClassificationId)
                {
                    var bidMainClassificationMapping = new BidMainClassificationMapping();
                    bidMainClassificationMapping.BidId = bid.Id;
                    bidMainClassificationMapping.BidMainClassificationId = cid;
                    bidMainClassificationMapping.CreatedBy = usr.Id;
                    if (!(mainClassificationMappingLST.Where(a => a.BidMainClassificationId == cid).Count() > 0))
                        bidMainClassificationMappings.Add(bidMainClassificationMapping);
                }

                //  bid.BidMainClassificationMapping = bidMainClassificationMappings;
                await _bidRepository.Update(bid);
                await _bidMainClassificationMappingRepository.AddRange(bidMainClassificationMappings);
                return OperationResult<long>.Success(bid.Id);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = model,
                    ErrorMessage = "Failed to Add Bid Classification Area And Execution!",
                    ControllerAndAction = "BidController/AddBidClassificationAreaAndExecution"
                });
                return OperationResult<long>.Fail(
                       HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }


        //public async Task<OperationResult<long>> AddBidNews(AddBidNewsModel model)
        //    => await _bidServiceCore.AddBidNews(model);
        public async Task<OperationResult<long>> AddBidNews(AddBidNewsModel model)
        {
            var usr = _currentUserService.CurrentUser;
            if (usr == null && usr.UserType != UserType.Association)
            {
                return OperationResult<long>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);
            }

            try
            {
                long bidNewsId = 0;
                var bid = await _bidRepository.FindOneAsync(x => x.Id == model.BidId, false);

                if (bid == null)
                {
                    return OperationResult<long>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.INVALID_BID);
                }

                var association = await _associationService.GetUserAssociation(usr.Email);
                if (association == null)
                    return OperationResult<long>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.ASSOCIATION_NOT_FOUND);

                if (model.Id != 0)
                {
                    var bidNews = await _bidNewsRepository.FindOneAsync(x => x.Id == model.Id, false);

                    if (bidNews == null)
                    {
                        return OperationResult<long>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.INVALID_BID_ADDRESSES_TIME);
                    }
                    //if (usr.Id != bid.CreatedBy)
                    //    return OperationResult<long>.Fail(HttpErrorCode.NotAuthorized, "to edit bid You must be the person who creates it.");

                    bidNews.Title = model.Title;
                    bidNews.InsertedDate = _dateTimeZone.CurrentDate;

                    bidNews.Image = model.ImageUrl;
                    bidNews.ImageFileName = model.ImageUrlFileName;
                    bidNews.Details = model.Details;

                    await _bidNewsRepository.Update(bidNews);
                }
                else
                {
                    var entity = _mapper.Map<BidNews>(model);

                    await _bidNewsRepository.Add(entity);
                    bidNewsId = entity.Id;
                }

                return OperationResult<long>.Success(model.BidId);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = model,
                    ErrorMessage = "Failed to Add Bid News!",
                    ControllerAndAction = "BidController/AddBidNews"
                });
                return OperationResult<long>.Fail(
                       HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }


        //public async Task<OperationResult<long>> TenderExtend(AddBidAddressesTimesTenderExtendModel model)
        //    => await _bidServiceCore.TenderExtend(model);
        public async Task<OperationResult<long>> TenderExtend(AddBidAddressesTimesTenderExtendModel model)
        {
            try
            {
                var usr = _currentUserService.CurrentUser;

                var authorizedTypes = new List<UserType>() { UserType.Association, UserType.Donor, UserType.Admin, UserType.SuperAdmin };

                if (usr == null || !authorizedTypes.Contains(usr.UserType))
                    return OperationResult<long>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);

                long bidAddressesTimesId = 0;
                var bid = await _bidRepository
                    .Find(x => x.Id == model.BidId)
                    .IncludeBasicBidData()
                    .FirstOrDefaultAsync();

                if (bid == null)
                    return OperationResult<long>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);

                if (bid.BidStatusId == (int)TenderStatus.Cancelled)
                    return OperationResult<long>.Fail(HttpErrorCode.BusinessRuleViolation, CommonErrorCodes.YOU_CAN_NOT_EXTEND_CANCELLED_BID);


                if (bid.BidTypeId != (int)BidTypes.Instant && bid.BidAddressesTime == null)
                    return OperationResult<long>.Fail(HttpErrorCode.NotFound, BidErrorCodes.BID_ADDRESSES_TIMES_HAS_NO_DATA);

                Association association = null;
                if (usr.UserType == UserType.Association)
                {
                    association = await _associationService.GetUserAssociation(usr.Email);
                    if (association is null)
                        return OperationResult<long>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.ASSOCIATION_NOT_FOUND);

                    if ((bid.EntityType != UserType.Association) || (bid.EntityId != usr.CurrentOrgnizationId))
                        return OperationResult<long>.Fail(HttpErrorCode.NotAuthorized, CommonErrorCodes.NotAuthorized);
                }

                Donor donor = null;
                if (usr.UserType == UserType.Donor)
                {
                    donor = await _donorService.GetUserDonor(usr.Email);
                    if (donor is null)
                        return OperationResult<long>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.DONOR_NOT_FOUND);

                    if ((bid.EntityType != UserType.Donor) || (bid.EntityId != usr.CurrentOrgnizationId))
                        return OperationResult<long>.Fail(HttpErrorCode.NotAuthorized, CommonErrorCodes.NotAuthorized);
                }

                var generalSettingsResult = await _appGeneralSettingService.GetAppGeneralSettings();
                if (!generalSettingsResult.IsSucceeded)
                    return OperationResult<long>.Fail(generalSettingsResult.HttpErrorCode, generalSettingsResult.Code);

                var generalSettings = generalSettingsResult.Data;

                if (model.OffersOpeningDate < model.LastDateInOffersSubmission)
                    return OperationResult<long>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.OFFERS_OPENING_DATE_MUST_BE_GREATER_THAN_LAST_DATE_IN_OFFERS_SUBMISSION);

                if (model.ExpectedAnchoringDate != null && model.ExpectedAnchoringDate != default
                     && model.ExpectedAnchoringDate < model.OffersOpeningDate.AddDays(bid.BidAddressesTime.StoppingPeriod))
                    return OperationResult<long>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.EXPECTED_ANCHORING_DATE_MUST_BE_GREATER_THAN_OFFERS_OPENING_DATE_PLUS_STOPPING_PERIOD);

                var bidAddressesTime = await _bidAddressesTimeRepository.FindOneAsync(x => x.BidId == model.BidId, false, nameof(BidAddressesTime.Bid));

                if (bidAddressesTime == null)
                    return OperationResult<long>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.INVALID_BID_ADDRESSES_TIME);
                var oldLastDateInOfferSubmission = string.Empty;
                if (bidAddressesTime != null)
                {
                    #region Log
                    var log = new BidAddressesTimeLog
                    {
                        BidId = bidAddressesTime.BidId,
                        OffersOpeningDate = (DateTime)bidAddressesTime.OffersOpeningDate,
                        LastDateInOffersSubmission = (DateTime)bidAddressesTime.LastDateInOffersSubmission,
                        ExpectedAnchoringDate = bidAddressesTime.ExpectedAnchoringDate ?? ((DateTime)bidAddressesTime.OffersOpeningDate).AddDays(bidAddressesTime.StoppingPeriod + 1),
                        CreatedBy = usr.Id,
                        CreationDate = _dateTimeZone.CurrentDate
                    };
                    await _bidAddressesTimeLogRepository.Add(log);
                    #endregion
                    oldLastDateInOfferSubmission = bid.BidAddressesTime.LastDateInOffersSubmission?.ToArabicFormat();
                    bidAddressesTimesId = bid.BidAddressesTime.Id;
                    bid.BidAddressesTime.BidId = model.BidId;
                    bid.BidAddressesTime.LastDateInOffersSubmission = new DateTime(model.LastDateInOffersSubmission.Year, model.LastDateInOffersSubmission.Month, model.LastDateInOffersSubmission.Day, 23, 59, 59);
                    bid.BidAddressesTime.OffersOpeningDate = model.OffersOpeningDate.Date;
                    bid.BidAddressesTime.ExpectedAnchoringDate = (model.ExpectedAnchoringDate != null && model.ExpectedAnchoringDate != default)
                        ? model.ExpectedAnchoringDate.Value.Date
                        : model.OffersOpeningDate.AddDays(bid.BidAddressesTime.StoppingPeriod + 1).Date;
                    bid.BidAddressesTime.IsTimeExtended = true;
                    bid.BidAddressesTime.ExtendedReason = model.ExtendReason;
                    bid.BidAddressesTime.ExtensionDate = _dateTimeZone.CurrentDate;

                    model.ExpectedAnchoringDate = (model.ExpectedAnchoringDate != null && model.ExpectedAnchoringDate != default)
                        ? model.ExpectedAnchoringDate.Value.Date
                        : model.OffersOpeningDate.AddDays(bid.BidAddressesTime.StoppingPeriod + 1).Date;

                    await _bidAddressesTimeRepository.Update(bid.BidAddressesTime);

                    var companiesBoughtTerms = await _providerBidRepository
                        .Find(b => b.IsPaymentConfirmed && b.BidId == bid.Id)
                        .Include(b => b.Company)
                            .ThenInclude(c => c.Provider)
                        .Select(b => b.Company)
                        .ToListAsync();

                    // var entityName = bid.EntityType == UserType.Association ? bid.Association?.Association_Name : bid.Donor?.DonorName;
                    var entityName = await _bidServiceCore.GetBidCreatorName(bid);

                    var companiesBoughtTermsUsersIds = new List<string>();
                    var companiesBoughtTermsIds = new List<string>();

                    var notifyByEMail = new SendEmailInBackgroundModel
                    {
                        EmailRequests = new List<ReadonlyEmailRequestModel>()
                    };
                    foreach (var item in companiesBoughtTerms)
                    {
                        var emailModel = new BidExtensionEmail()
                        {
                            BaseBidEmailDto = await _helperService.GetBaseDataForBidsEmails(bid),
                            OldLastDateInOfferSubmission = oldLastDateInOfferSubmission
                        };
                        var userEamil = await _companyUserRolesService.GetEmailReceiverForProvider(item.Id, item.Provider.Email);
                        var emailRequest = new EmailRequest()
                        {
                            ControllerName = BaseBidEmailDto.BidsEmailsPath,
                            ViewName = BidExtensionEmail.EmailTemplateName,
                            ViewObject = emailModel,
                            To = userEamil.Email,
                            Subject = $"????? ???? ???????? {bid.BidName}",
                            SystemEventType = (int)SystemEventsTypes.BidExtensionEmail
                        };
                        notifyByEMail.EmailRequests.Add(new ReadonlyEmailRequestModel() { EntityId = item.Id, EntityType = UserType.Company, EmailRequest = emailRequest });
                    }
                    // send email to admins
                    var adminsEmails = await _userManager.Users
                       .Where(u => u.UserType == UserType.SuperAdmin)
                       .Select(u => u.Email)
                       .ToListAsync();
                    var _commonEmailAndNotificationService = (ICommonEmailAndNotificationService)_serviceProvider.GetService(typeof(ICommonEmailAndNotificationService));
                    var adminPermissionUsers = await _commonEmailAndNotificationService.GetAdminClaimOfEmails(new List<AdminClaimCodes> { AdminClaimCodes.clm_2553 });
                    adminsEmails.AddRange(adminPermissionUsers);
                    var emailModel1 = new BidExtensionEmail()
                    {
                        BaseBidEmailDto = await _helperService.GetBaseDataForBidsEmails(bid),
                        OldLastDateInOfferSubmission = oldLastDateInOfferSubmission
                    };
                    var adminEmailRequest = new EmailRequestMultipleRecipients()
                    {
                        ControllerName = BaseBidEmailDto.BidsEmailsPath,
                        ViewName = BidExtensionEmail.EmailTemplateName,
                        ViewObject = emailModel1,
                        Recipients = adminsEmails.Select(s => new RecipientsUser { Email = s }).ToList(),
                        Subject = $"????? ???? ???????? {bid.BidName}",
                        SystemEventType = (int)SystemEventsTypes.BidExtensionEmail,
                    };

                    await _emailService.SendToMultipleReceiversAsync(adminEmailRequest);

                    var companyIds = companiesBoughtTerms.Select(x => x.Id).ToList();
                    _notifyInBackgroundService.SendEmailInBackground(notifyByEMail);
                    var notifyByNotification = new List<SendNotificationInBackgroundModel>()
                    {
                        new SendNotificationInBackgroundModel
                        {
                            IsSendToMultipleReceivers=true,
                            NotificationModel=new NotificationModel
                            {
                                BidId = bid.Id,
                                BidName = bid.BidName,
                                SenderName = entityName,
                                AssociationName = entityName,
                                NewBidExtendDate = model.LastDateInOffersSubmission,
                                EntityId = bid.Id,
                                Message = $"?? ????? ???? ????? ?????? ? {bid.BidName} ?????? ?????? {model.LastDateInOffersSubmission}	",
                                NotificationType = NotificationType.ExtendBid,
                                SenderId = usr.Id,
                                ServiceType=ServiceType.Bids
                            },
                            ClaimsThatUsersMustHaveToReceiveNotification= new List<string>{ProviderClaimCodes.clm_3041.ToString() },
                            ReceiversOrganizations=companyIds.Select(x=>(x,OrganizationType.Comapny)).ToList()
                           // ReceiversIds = await _notificationUserClaim.GetUsersClaimOfMultipleIds(new string[] { ProviderClaimCodes.clm_3041.ToString() } ,companyIds, OrganizationType.Comapny)
                        }
                    };
                    _notifyInBackgroundService.SendNotificationInBackground(notifyByNotification);
                    //===============log event===============
                    string[] styles = await _helperService.GetEventStyle(EventTypes.ExtendBid);
                    await _helperService.LogBidEvents(new BidEventModel
                    {
                        BidId = bid.Id,
                        BidStatus = (TenderStatus)bid.BidStatusId,
                        BidEventSection = BidEventSections.Bid,
                        BidEventTypeId = (int)EventTypes.ExtendBid,
                        EventCreationDate = _dateTimeZone.CurrentDate,
                        ActionId = bid.Id,
                        Audience = AudienceTypes.All,
                        Header = string.Format(styles[0], fileSettings.ONLINE_URL, bid.EntityType == UserType.Association ? "association" : "donor", bid.EntityId, entityName, _dateTimeZone.CurrentDate.ToString("dddd d MMMM? yyyy , h:mm tt", new CultureInfo("ar-AE"))),
                        Notes1 = string.Format(styles[1], model.LastDateInOffersSubmission.ToString("d MMMM? yyyy", new CultureInfo("ar-AE"))),
                        //Notes2 = string.Format(styles[2], model.ExtensionReason),
                    });
                }
                return OperationResult<long>.Success(model.BidId);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = model,
                    ErrorMessage = "Failed to Extend Bid Addresses Times!",
                    ControllerAndAction = "BidController/tender-extend"
                });
                return OperationResult<long>.Fail(HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }


        //public async Task<OperationResult<bool>> CopyBid(CopyBidRequest model)
        //    => await _bidServiceCore.CopyBid(model);
        public async Task<OperationResult<bool>> CopyBid(CopyBidRequest model)
        {
            try
            {
                var usr = _currentUserService.CurrentUser;
                var authorizedTypes = new List<UserType>() { UserType.Association, UserType.Donor, UserType.SuperAdmin };
                if (usr is null)
                    return OperationResult<bool>.Fail(HttpErrorCode.NotAuthenticated);
                if (usr == null || !authorizedTypes.Contains(usr.UserType))
                    return OperationResult<bool>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);

                Bid bid = await _bidRepository.Find(x => x.Id == model.BidId, true, false)
                    .Include(b => b.Bid_Industries)
                    .Include(a => a.FreelanceBidIndustries)
                    .Include(b => b.Association)
                    .Include(b => b.Donor)
                    .Include(b => b.BidRegions)
                    .Include(b => b.QuantitiesTable)
                    .Include(b => b.BidDonor)
                    .Include(b => b.BidAttachment)
                    .Include(b => b.BidInvitations)
                    .Include(b => b.BidAchievementPhases)
                        .ThenInclude(b => b.BidAchievementPhaseAttachments)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync();

                if (bid == null)
                    return OperationResult<bool>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.INVALID_BID);

                if (usr.UserType != UserType.SuperAdmin && (bid.EntityId != usr.CurrentOrgnizationId || bid.EntityType != usr.UserType))
                    return OperationResult<bool>.Fail(HttpErrorCode.NotAuthorized, CommonErrorCodes.NotAuthorized);

                //generate code
                string firstPart_Ref_Number = DateTime.Now.ToString("yy") + DateTime.Now.ToString("MM") + bid.BidTypeId.ToString();
                string randomNumber = await _bidServiceCore.GenerateBidRefNumber(bid.Id, firstPart_Ref_Number);

                var copyBid = new Bid
                {
                    Ref_Number = randomNumber,
                    BidName = model.NewBidName,
                    Bid_Number = bid.Bid_Number,
                    Objective = bid.Objective,
                    IsDeleted = false,
                    AssociationId = bid.AssociationId,
                    DonorId = bid.DonorId,
                    BidStatusId = (int)TenderStatus.Draft,
                    IsInvitationNeedAttachments = bid.IsInvitationNeedAttachments,
                    BidOffersSubmissionTypeId = bid.BidOffersSubmissionTypeId,
                    BidTypeId = bid.BidTypeId,
                    BidVisibility = (BidTypes)bid.BidTypeId.Value,
                    EntityType = bid.EntityType,
                    EntityId = bid.EntityId,
                    Bid_Documents_Price = bid.Bid_Documents_Price,
                    Tanafos_Fees = bid.Tanafos_Fees,
                    Association_Fees = bid.Association_Fees,
                    IsFunded = bid.IsFunded,
                    IsBidAssignedForAssociationsOnly = bid.IsBidAssignedForAssociationsOnly,
                    IsAssociationFoundToSupervise = bid.IsAssociationFoundToSupervise,
                    SupervisingAssociationId = bid.SupervisingAssociationId,
                    BidTypeBudgetId = await _bidServiceCore.MapBidTypeBudgetId(bid),
                    IsFinancialInsuranceRequired = bid.IsFinancialInsuranceRequired,
                    FinancialInsuranceValue = bid.FinancialInsuranceValue,
                    //bid Attachments
                    TenderBrochurePoliciesType = bid.TenderBrochurePoliciesType,
                    Tender_Brochure_Policies_Url = bid.Tender_Brochure_Policies_Url,
                    Tender_Brochure_Policies_FileName = bid.Tender_Brochure_Policies_FileName,

                    CreatedBy = usr.Id,
                    CreationDate = _dateTimeZone.CurrentDate
                };
                await _bidRepository.Add(copyBid);

                #region add Bid Regions
                await _bidServiceCore.AddBidRegions(bid.BidRegions.Select(a => a.RegionId).ToList(), copyBid.Id);
                #endregion

                #region add Bid Commerical Sectors
                List<Bid_Industry> bidIndustries = new List<Bid_Industry>();
                foreach (var cid in bid.Bid_Industries)
                {
                    var bidIndustry = new Bid_Industry();
                    bidIndustry.BidId = copyBid.Id;
                    bidIndustry.CommercialSectorsTreeId = cid.CommercialSectorsTreeId;
                    bidIndustry.CreatedBy = usr.Id;
                    bidIndustries.Add(bidIndustry);
                }
                await _bidIndustryRepository.AddRange(bidIndustries);
                List<FreelanceBidIndustry> FreelanceBidIndustries = new List<FreelanceBidIndustry>();
                foreach (var cid in bid.FreelanceBidIndustries)
                {
                    var FreelanceBidIndustry = new FreelanceBidIndustry();
                    FreelanceBidIndustry.BidId = copyBid.Id;
                    FreelanceBidIndustry.FreelanceWorkingSectorId = cid.FreelanceWorkingSectorId;
                    FreelanceBidIndustry.CreatedBy = usr.Id;
                    FreelanceBidIndustries.Add(FreelanceBidIndustry);
                }
                await  _freelanceBidIndustryRepository.AddRange(FreelanceBidIndustries);
                #endregion

                #region add Bid Donner
                if (bid.IsFunded && bid.BidDonor is not null)
                {
                    BidDonorRequest bidDonorRequest = new BidDonorRequest();
                    bidDonorRequest.DonorId = bid.BidDonor.DonorId.GetValueOrDefault();
                    bidDonorRequest.NewDonorName = bid.BidDonor.NewDonorName;
                    bidDonorRequest.Email = bid.BidDonor.Email;
                    bidDonorRequest.PhoneNumber = bid.BidDonor.PhoneNumber;
                    var res = await _bidServiceCore.SaveBidDonor(bidDonorRequest, copyBid.Id, usr.Id);
                }
                #endregion

                #region AddInvitationToAssocationByDonorIfFound
                if (usr.UserType == UserType.Donor)
                {
                    var invitedAssociation = await _invitedAssociationsByDonorRepository.FindOneAsync(inv => inv.BidId == bid.Id);
                    if (invitedAssociation is not null)
                    {
                        InvitedAssociationByDonorModel invitedAssociationByDonorModel = new InvitedAssociationByDonorModel();
                        invitedAssociationByDonorModel.AssociationName = invitedAssociation.AssociationName;
                        invitedAssociationByDonorModel.Email = invitedAssociation.Email;
                        invitedAssociationByDonorModel.Registry_Number = invitedAssociation.Registry_Number;
                        invitedAssociationByDonorModel.Mobile = invitedAssociation.Mobile;
                        var res = await _bidServiceCore.AddInvitationToAssocationByDonorIfFound(invitedAssociationByDonorModel, copyBid, bid.IsAssociationFoundToSupervise, bid.SupervisingAssociationId);
                    }
                }
                #endregion

                #region SendNewDraftBidEmailToSuperAdmins
                var entityName = bid.AssociationId.HasValue ? bid.Association.Association_Name : bid.Donor.DonorName;
                await _bidServiceCore.SendNewDraftBidEmailToSuperAdmins(copyBid, entityName);
                #endregion

                #region add Bid Quantities Table 
                var quantitiesTable = new List<QuantitiesTable>();

                foreach (var table in bid.QuantitiesTable)
                {
                    quantitiesTable.Add(new QuantitiesTable
                    {
                        BidId = copyBid.Id,
                        ItemNo = table.ItemNo,
                        Category = table.Category,
                        ItemName = table.ItemName,
                        ItemDesc = table.ItemDesc,
                        Quantity = table.Quantity,
                        Unit = table.Unit,
                    });
                }
                await _bidQuantitiesTableRepository.AddRange(quantitiesTable);
                #endregion

                #region add Bid Attachments         
                var bidAttachmentsToSave = new List<BidAttachment>();
                if (bid.BidAttachment.Any())
                {
                    foreach (var attachment in bid.BidAttachment)
                    {
                        bidAttachmentsToSave.Add(new BidAttachment
                        {
                            BidId = copyBid.Id,
                            AttachmentName = attachment.AttachmentName,
                            AttachedFileURL = attachment.AttachedFileURL,
                            IsDeleted = false
                        });
                    }
                    await _bidAttachmentRepository.AddRange(bidAttachmentsToSave);
                }
                #endregion

                #region Add Bid Invitation
                //var allBidInvitation = await _bidInvitationsRepository.FindAsync(a => a.BidId == model.BidId);
                //List<BidInvitations> newInvitations = new List<BidInvitations>();
                //foreach (var item in bid.BidInvitations)
                //{
                //    newInvitations.Add(new BidInvitations
                //    {
                //        BidId = copyBid.Id,
                //        Email = item.Email,
                //        PhoneNumber = item.PhoneNumber,
                //        CommercialNo = item.CommercialNo,
                //        CompanyId = item.CompanyId,
                //        ManualCompanyId = item.ManualCompanyId,
                //        InvitationType = InvitationType.Private,
                //        InvitationStatus = InvitationStatus.New,
                //        CreationDate = _dateTimeZone.CurrentDate,
                //        CreatedBy = usr.Id
                //    });
                //}
                //await _bidInvitationsRepository.AddRange(newInvitations);
                #endregion

                await _bidServiceCore.CopyBidAchievementPhasesPhases(bid, copyBid);

                //==========================response===========================
                return OperationResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = model,
                    ErrorMessage = "Failed to Copy Bid!",
                    ControllerAndAction = "BidController/CopyBid"
                });
                return OperationResult<bool>.Fail(HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }


        //public async Task<OperationResult<bool>> DeleteDraftBid(long bidId)
        //    => await _bidServiceCore.DeleteDraftBid(bidId);
        public async Task<OperationResult<bool>> DeleteDraftBid(long bidId)
        {
            try
            {

                var currentUser = _currentUserService.CurrentUser;

                var allowedUserTypesToDeleteBid = new List<UserType>() { UserType.SuperAdmin, UserType.Admin, UserType.Association, UserType.Donor };
                if (currentUser is null || !allowedUserTypesToDeleteBid.Contains(currentUser.UserType))
                    return OperationResult<bool>.Fail(HttpErrorCode.NotAuthorized, CommonErrorCodes.NotAuthorized);

                var bid = await _bidRepository.GetById(bidId);
                var allowedBidTypesToBeDeleted = new List<int>() { (int)TenderStatus.Reviewing, (int)TenderStatus.Draft };
                if (bid is null || !allowedBidTypesToBeDeleted.Contains(bid.BidStatusId ?? 0))
                    return OperationResult<bool>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);

                if (currentUser.UserType == UserType.Association)
                {
                    var associationOfCurrentUser = await _associationService.GetUserAssociation(currentUser.Email);
                    if (associationOfCurrentUser is null)
                        return OperationResult<bool>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.ASSOCIATION_NOT_FOUND);

                    if (associationOfCurrentUser.Id != bid.EntityId || bid.EntityType != UserType.Association)
                        return OperationResult<bool>.Fail(HttpErrorCode.Conflict, CommonErrorCodes.ASSOCIATION_CAN_ONLY_DELETE_ITS_BIDS);
                }
                if (currentUser.UserType == UserType.Donor)
                {
                    var donor = await _bidServiceCore.GetDonorUser(currentUser);
                    if (donor is null)
                        return OperationResult<bool>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.DONOR_NOT_FOUND);

                    if (donor.Id != bid.EntityId || bid.EntityType != UserType.Donor)
                        return OperationResult<bool>.Fail(HttpErrorCode.Conflict, CommonErrorCodes.YOU_CAN_NOT_DO_THIS_ACTION_BECAUSE_YOU_ARE_NOT_THE_CREATOR);
                }
                return OperationResult<bool>.Success(await _bidRepository.Delete(bid));

            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = $"Bid ID = {bidId}",
                    ErrorMessage = "Failed to Delete bid !",
                    ControllerAndAction = "BidController/DeleteBid/{bidId}"
                });
                return OperationResult<bool>.Fail(
                       HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }

    }
}
