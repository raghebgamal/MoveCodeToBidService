using AutoMapper;
using CommunityToolkit.HighPerformance;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2016.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
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
using Nafes.CrossCutting.Domain.Integration.HyperPay;
using Nafes.CrossCutting.Domain.Integration.PayTabs;
using Nafes.CrossCutting.Model.CommonModels;
using Nafes.CrossCutting.Model.Entities;
using Nafes.CrossCutting.Model.Entities.Market;
using Nafes.CrossCutting.Model.Enums;
using Nafes.CrossCutting.Model.Enums.Extensions;
using Nafes.CrossCutting.Model.Enums.NICEnums;
using Nafes.CrossCutting.Model.Lookups;
using Nafis.Services.Contracts;
using Nafis.Services.Contracts.CommonServices;
using Nafis.Services.Contracts.Factories;
using Nafis.Services.Contracts.Repositories;
using Nafis.Services.DTO;
using Nafis.Services.DTO.AppGeneralSettings;
using Nafis.Services.DTO.Association;
using Nafis.Services.DTO.AssociationWithdraw;
using Nafis.Services.DTO.Bid;
using Nafis.Services.DTO.BidAnnouncement;
using Nafis.Services.DTO.BuyTenderDocsPill;
using Nafis.Services.DTO.CommonServices;
using Nafis.Services.DTO.Company;
using Nafis.Services.DTO.Coupon;
using Nafis.Services.DTO.Donor;
using Nafis.Services.DTO.Notification;
using Nafis.Services.DTO.Payment;
using Nafis.Services.DTO.Provider;
using Nafis.Services.DTO.Ratings;
using Nafis.Services.DTO.Sendinblue;
using Nafis.Services.Extensions;
using Nafis.Services.Hubs;
using Nafis.Services.Implementation.CommonServices.NotificationHelper;
using Nafis.Services.Implementation.Hangfire;
using RedLockNet;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tanafos.Main.Services.Contracts;
using Tanafos.Main.Services.Contracts.CommonServices;
using Tanafos.Main.Services.DTO;
using Tanafos.Main.Services.DTO.Bid;
using Tanafos.Main.Services.DTO.BidAddresses;
using Tanafos.Main.Services.DTO.ChannelDTOs;
using Tanafos.Main.Services.DTO.Emails.Bids;
using Tanafos.Main.Services.DTO.FinancialTransactionPartiesPercentage;
using Tanafos.Main.Services.DTO.Point;
using Tanafos.Main.Services.DTO.ReviewedSystemRequestLog;
using Tanafos.Main.Services.DTO.SubscriptionAddOns;
using Tanafos.Main.Services.Extensions;
using Tanafos.Main.Services.Implementation.CommonServices;
using Tanafos.Main.Services.Implementation.Services;
using Tanafos.Shared.Service.Contracts.CommonServices;
using Tanafos.Shared.Service.DTO.CommonServices;
using static Nafes.CrossCutting.Common.Helpers.Constants;
using static Nafes.CrossCutting.Model.Enums.BidAchievementPhasesEnums;
using static Nafes.CrossCutting.Model.Enums.BidEventsEnum;
using static Nafis.Services.DTO.Bid.AddBidModel;
using static QRCoder.PayloadGenerator;
using Company = Nafes.CrossCutting.Model.Entities.Company;
using Contract = Nafes.CrossCutting.Model.Entities.Contract;
using TermsBookPrice = Nafes.CrossCutting.Model.Enums.TermsBookPrice;

namespace Nafis.Services.Implementation
{
   
    public class BidServiceCore
    {
        private readonly ICrossCuttingRepository<Bid, long> _bidRepository;
        private readonly ICrossCuttingRepository<RFP, long> _rfpRepository;
        private readonly ICrossCuttingRepository<Evaluation, long> _evaluationRepository;
        private readonly ICrossCuttingRepository<Donor, long> _donorRepository;
        private readonly ICrossCuttingRepository<BidRegion, int> _bidRegionsRepository;
        private readonly ICrossCuttingRepository<Nafes.CrossCutting.Model.Lookups.Region, int> _regionRepository;
        private readonly ICrossCuttingRepository<Inquiry, long> _inquiryRepository;
        private readonly ICrossCuttingRepository<TenderSubmitQuotation, long> _tenderSubmitQuotationRepository;
        private readonly ICrossCuttingRepository<UserFavBidList, long> _userFavBidList;
        private readonly ILoggerService<BidService> _logger;
        private readonly IMapper _mapper;
        private readonly IHelperService _helperService;
        private readonly IRandomGeneratorService _randomGeneratorService;
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
        private readonly ICrossCuttingRepository<RFIRequest, long> _rFIRequestRepository;
        private readonly GeneralSettings _generalSettings;
        private readonly ICrossCuttingRepository<ProviderBid, long> _providerBidRepository;
        private readonly ICrossCuttingRepository<CompanyInvitationDocument, long> _companyInvitationDocumentRepository;
        private readonly ICrossCuttingRepository<Provider, long> _providerRepository;
        private readonly ICrossCuttingRepository<BidInvitations, long> _bidInvitationsRepository;
        private readonly ICompanyService _companyService;
        private readonly ICrossCuttingRepository<Company, long> _companyRepository;
        private readonly ICrossCuttingRepository<Freelancer, long> _freelancerRepository;
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
        private readonly ICrossCuttingRepository<Nafes.CrossCutting.Model.Lookups.BidStatus, long> _bidStatusRepository;
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
        private readonly ICompanyNICService _companyNICService;
        private readonly ICrossCuttingRepository<IntegrativeServices, long> _integrativeServicesRepository;
        private readonly ICrossCuttingRepository<BidType, int> _bidTypeRepository;
        private readonly ISendinblueService _sendinblueService;
        private readonly SendinblueOptions _sendinblueOptions;
        private readonly INotifyInBackgroundService _notifyInBackgroundService;
        private readonly ICrossCuttingRepository<ProviderRefundTransaction, long> _providerRefundTransactionRepository;
        private readonly ICrossCuttingRepository<BidSupervisingData, long> _bidSupervisingDataRepository;
        private readonly INotificationUserClaim _notificationUserClaim;
        private readonly IPaymentGatewayFactory _paymentGatewayFactory;
        private readonly ICrossCuttingRepository<PaymentGatewaySetting, int> _paymentGatewaySettingRepository;
        private readonly ICrossCuttingRepository<HyperPayTransaction, long> _hyperPayTransactionRepository;
        private readonly ICrossCuttingRepository<Coupon, long> _couponRepository;
        private readonly ICrossCuttingRepository<CouponUsagesHistory, long> _couponUsageHistoryRepository;
        private readonly ICouponServiceCommonMethodsForPayments _bidAndCouponServicesCommonMethods;
        private readonly ICrossCuttingRepository<BidAchievementPhases, long> _bidAchievementPhasesRepository;
        private readonly ICrossCuttingRepository<CancelBidRequest, long> _cancelBidRequestRepository;
        private readonly ICrossCuttingRepository<BIdWithHtml, long> _bIdWithHtmlRepository;
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
        private readonly IInvoiceService _invoiceService;
        private readonly ICrossCuttingRepository<OrganizationUser, long> _organizationUserRepository;
        private readonly IBidSearchLogService _bidSearchLogService;
        private readonly ICrossCuttingRepository<ManualCompany, long> _manualCompanyRepository;
        private readonly ICrossCuttingRepository<SubscriptionPayment, long> _subscriptionPaymentRepository;
        private readonly ISubscriptionAddonsService _subscriptionAddonsService;
        private readonly IChannelWriter<GenerateTenderDocsPillModel> _channelWriterTenderDocs;
        private readonly ICommonEmailAndNotificationService _commonEmailAndNotificationService;
        private readonly ICrossCuttingRepository<FreelanceBidIndustry, long> _freelanceBidIndustryRepository;
        private readonly ICrossCuttingRepository<FreelancerFreelanceWorkingSector, long> _freelancerFreelanceWorkingSectorRepository;
        private readonly ICrossCuttingRepository<SubscriptionPaymentFeature, long> _subscriptionPaymentFeatureRepository;
        private readonly ICrossCuttingRepository<SubscriptionPaymentFeatureUsage, long> _subscriptionPaymentFeatureUsageRepository;
        private readonly ICrossCuttingRepository<BidRevealLog, long> _bidRevealLogRepository;



        public BidServiceCore(ICrossCuttingRepository<HyperPayTransaction, long> hyperPayTransactionRepository,
            IAssociationService associationService,
            ICrossCuttingRepository<Bid, long> bidRepository,
            //ICommonEmailAndNotificationService commonEmailAndNotificationService,
            ICrossCuttingRepository<BidRegion, int> bidRegionsRepository,
            ICrossCuttingRepository<Nafes.CrossCutting.Model.Lookups.Region, int> regionRepository,
            ICrossCuttingRepository<Inquiry, long> inquiryRepository,
            ICrossCuttingRepository<TenderSubmitQuotation, long> tenderSubmitQuotationRepository,
            ILoggerService<BidService> logger,
            IMapper mapper,
            IHelperService helperService,
            IRandomGeneratorService randomGeneratorService,
            ICurrentUserService currentUserService,
            ICrossCuttingRepository<Association, long> associationRepository,
            ICrossCuttingRepository<BidAddressesTime, long> bidAddressesTimeRepository,
            ICrossCuttingRepository<QuantitiesTable, long> bidQuantitiesTableRepository,
            ICrossCuttingRepository<BidAttachment, long> bidAttachmentRepository,
            IOptions<FileSettings> FileSettings,
            IImageService _imageService,
            ICrossCuttingRepository<BidNews, long> bidNewsRepository,
            ICrossCuttingRepository<UserFavBidList, long> userFavBidList,
            ICrossCuttingRepository<Association_Additional_Contact_Detail, int> associationAdditional_ContactRepository,
            ICrossCuttingRepository<RFIRequest, long> rFIRequestRepository,
            IOptions<GeneralSettings> generalSettings,
            ICrossCuttingRepository<ProviderBid, long> providerBidRepository,
            ICrossCuttingRepository<Provider, long> providerRepository,
            //ITenderSubmitQuotationService tenderSubmitQuotationService,
            ICrossCuttingRepository<BidInvitations, long> bidInvitationsRepository,
            ICrossCuttingRepository<CompanyInvitationDocument, long> companyInvitationDocumentRepository,
            ICrossCuttingRepository<BidAddressesTimeLog, long> bidAddressesTimeLogRepository,
            ICrossCuttingRepository<ProviderBidExtension, long> providerBidExtensionRepository,
            ICompanyService companyService,
            ICrossCuttingRepository<Company, long> companyRepository,
            ICrossCuttingRepository<AwardingSelect, long> awardingSelectRepository,
            ICrossCuttingRepository<Bid_Industry, long> bidIndustryRepository,
            ICrossCuttingRepository<Company_Industry, long> companyIndustryRepository,
            ICrossCuttingRepository<Notification, long> notificationRepository,
            ICrossCuttingRepository<NotificationRecivers, long> notificationReciversRepository,
            IHubContext<NotificationHub> notificationHubContext,
            //INotificationService notificationService,
            IEmailService emailService,
            UserManager<ApplicationUser> userManager,
            IAppGeneralSettingService appGeneralSettingService,
            ICrossCuttingRepository<BidMainClassificationMapping, long> bidMainClassificationMappingRepository,
            IDateTimeZone dateTimeZone,
            ICompressService compressService,
            ICrossCuttingRepository<InvitationRequiredDocument, long> invitationRequiredDocumentRepository,
            ITenderSubmitQuotationRepositoryAsync bidsOfProviderRepository,
            ICrossCuttingRepository<Nafes.CrossCutting.Model.Lookups.BidStatus, long> bidStatusRepository,
            ICrossCuttingRepository<Industry, long> industryRepository,
            ICrossCuttingRepository<AwardingProvider, long> awardingProviderRepository,
            ICrossCuttingRepository<ProviderQuantitiesTableDetails, long> providerQuantitiesTableDetailsRepository,
            ICrossCuttingRepository<DemoSettings, long> demoSettingsRepository,
            ICrossCuttingRepository<Contract, long> contractRepository,
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
            ICrossCuttingRepository<CommercialSectorsTree, long> CommercialSectorsTreeRepository,
            ICrossCuttingRepository<InvitedAssociationsByDonor, long> invitedAssociationsByDonorRepository,
            ICrossCuttingRepository<BidDonor, long> BidDonorRepository,
            ICrossCuttingRepository<Donor, long> donorRepository,
            IDonorService donorService,
            ICompanyNICService companyNICService,
            ICrossCuttingRepository<IntegrativeServices, long> integrativeServicesRepository,
            ICrossCuttingRepository<BidType, int> bidTypeRepository,
            ISendinblueService sendinblueService,
            IOptions<SendinblueOptions> sendinblueOptions,
            INotifyInBackgroundService notifyInBackgroundService,
            ICrossCuttingRepository<ProviderRefundTransaction, long> providerRefundTransactionRepository,
            ICrossCuttingRepository<BidSupervisingData, long> bidSupervisingDataRepository,
            INotificationUserClaim notificationUserClaim,
            ICrossCuttingRepository<RFP, long> rfpRepository,
            IPaymentGatewayFactory paymentGatewayFactory,
            ICrossCuttingRepository<PaymentGatewaySetting, int> PaymentGatewayRepository,
            ICrossCuttingRepository<Evaluation, long> evaluationRepository,
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
            ISubscriptionAddonsService subscriptionAddonsService,
            IChannelWriter<GenerateTenderDocsPillModel> channelWriterTenderDocs,
            ICommonEmailAndNotificationService commonEmailAndNotificationService,
            ICrossCuttingRepository<SubscriptionPayment, long> subscriptionPaymentRepository,
            ICrossCuttingRepository<FreelanceBidIndustry, long> freelanceBidIndustryRepository,
            ICrossCuttingRepository<Freelancer, long> freelancerRepository,
            ICrossCuttingRepository<FreelancerFreelanceWorkingSector, long> freelancerFreelanceWorkingSectorRepository,
            ICrossCuttingRepository<SubscriptionPaymentFeature, long> subscriptionPaymentFeatureRepository,
            ICrossCuttingRepository<SubscriptionPaymentFeatureUsage, long> subscriptionPaymentFeatureUsageRepository,
            ICrossCuttingRepository<BidRevealLog, long> bidRevealLogRepository

            )
        {
            _hyperPayTransactionRepository = hyperPayTransactionRepository;
            _associationService = associationService;
            _donorRepository = donorRepository;
            //_commonEmailAndNotificationService = commonEmailAndNotificationService;
            _associationRepository = associationRepository;
            _tenderSubmitQuotationRepository = tenderSubmitQuotationRepository;
            _providerBidRepository = providerBidRepository;
            _bidRepository = bidRepository;
            _bidRegionsRepository = bidRegionsRepository;
            _regionRepository = regionRepository;
            _inquiryRepository = inquiryRepository;
            _userFavBidList = userFavBidList;
            _userManager = userManager;
            _bidAddressesTimeRepository = bidAddressesTimeRepository;
            _providerBidExtensionRepository = providerBidExtensionRepository;
            _bidQuantitiesTableRepository = bidQuantitiesTableRepository;
            _bidAttachmentRepository = bidAttachmentRepository;
            _companyInvitationDocumentRepository = companyInvitationDocumentRepository;
            _logger = logger;
            _mapper = mapper;
            _helperService = helperService;
            _randomGeneratorService = randomGeneratorService;
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            fileSettings = FileSettings.Value;
            this._imageService = _imageService ?? throw new ArgumentNullException(nameof(_imageService));
            _bidNewsRepository = bidNewsRepository;
            _associationAdditional_ContactRepository = associationAdditional_ContactRepository;
            _rFIRequestRepository = rFIRequestRepository;
            _generalSettings = generalSettings.Value;
            _providerRepository = providerRepository;
            //  _tenderSubmitQuotationService = tenderSubmitQuotationService;
            _bidInvitationsRepository = bidInvitationsRepository;
            _bidAddressesTimeLogRepository = bidAddressesTimeLogRepository;
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
            //_notificationService = notificationService;
            this._compressService = compressService;
            _invitationRequiredDocumentRepository = invitationRequiredDocumentRepository;
            _userManager = userManager;
            _bidsOfProviderRepository = bidsOfProviderRepository;
            _bidStatusRepository = bidStatusRepository;
            _industryRepository = industryRepository;
            _awardingProviderRepository = awardingProviderRepository;
            _providerQuantitiesTableDetailsRepository = providerQuantitiesTableDetailsRepository;
            _demoSettingsRepository = demoSettingsRepository;
            _contractRepository = contractRepository;
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
            _CommercialSectorsTreeRepository = CommercialSectorsTreeRepository;
            _invitedAssociationsByDonorRepository = invitedAssociationsByDonorRepository;
            _BidDonorRepository = BidDonorRepository;
            _donorService = donorService;
            _companyNICService = companyNICService;
            _integrativeServicesRepository = integrativeServicesRepository;
            _bidTypeRepository = bidTypeRepository;
            _sendinblueService = sendinblueService;
            _sendinblueOptions = sendinblueOptions.Value;
            this._notifyInBackgroundService = notifyInBackgroundService;
            _providerRefundTransactionRepository = providerRefundTransactionRepository;
            _bidSupervisingDataRepository = bidSupervisingDataRepository;
            _notificationUserClaim = notificationUserClaim;
            _rfpRepository = rfpRepository;
            _evaluationRepository = evaluationRepository;
            _paymentGatewayFactory = paymentGatewayFactory;
            _paymentGatewaySettingRepository = PaymentGatewayRepository;
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
            _subscriptionAddonsService = subscriptionAddonsService;
            _channelWriterTenderDocs = channelWriterTenderDocs;
            _subscriptionPaymentRepository = subscriptionPaymentRepository;
            _commonEmailAndNotificationService = commonEmailAndNotificationService;
            _freelancerRepository = freelancerRepository;
            _freelanceBidIndustryRepository = freelanceBidIndustryRepository;
            _freelancerFreelanceWorkingSectorRepository = freelancerFreelanceWorkingSectorRepository;
            _subscriptionPaymentFeatureRepository = subscriptionPaymentFeatureRepository;
            _subscriptionPaymentFeatureUsageRepository = subscriptionPaymentFeatureUsageRepository;
            _bidRevealLogRepository = bidRevealLogRepository;
        }


        

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

                var adjustBidAddressesToTheEndOfDayResult = AdjustRequestBidAddressesToTheEndOfTheDay(model);
                if (!adjustBidAddressesToTheEndOfDayResult.IsSucceeded)
                    return OperationResult<AddBidResponse>.Fail(adjustBidAddressesToTheEndOfDayResult.HttpErrorCode, adjustBidAddressesToTheEndOfDayResult.Code);

                if (IsRequiredDataForNotSaveAsDraftAdded(model))
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
                    donor = await GetDonorUser(usr);
                    if (donor == null)
                        return OperationResult<AddBidResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.DONOR_NOT_FOUND);
                }


                ValidateBidFinancialValueWithBidType(model);

                if (model.Id != 0)
                {
                    if (ValidateBidInvitationAttachmentsNew(model))
                    {
                        return OperationResult<AddBidResponse>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.ADDING_INVITATION_ATTACHMENTS_REQUIRED);
                    }

                    var bid = await _bidRepository.FindOneAsync(x => x.Id == model.Id, false, nameof(Bid.Bid_Industries)
                        , nameof(Bid.Association), nameof(Bid.BidAddressesTime), nameof(Bid.BidSupervisingData));
                    if (bid == null)
                        return OperationResult<AddBidResponse>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.INVALID_BID);

                    var validationOfBidDates = ValidateBidDates(model, bid, generalSettings);
                    if (!validationOfBidDates.IsSucceeded)
                        return OperationResult<AddBidResponse>.Fail(validationOfBidDates.HttpErrorCode, validationOfBidDates.Code, validationOfBidDates.ErrorMessage);

                    if ((usr.UserType != UserType.SuperAdmin && usr.UserType != UserType.Admin)
                        && (bid.EntityId != usr.CurrentOrgnizationId || bid.EntityType != usr.UserType))
                        return OperationResult<AddBidResponse>.Fail(HttpErrorCode.NotAuthorized, CommonErrorCodes.NotAuthorized);


                    if (usr.UserType == UserType.SuperAdmin || usr.UserType != UserType.Admin || usr.UserType == UserType.Donor)
                    {
                        var res = await this.AddInvitationToAssocationByDonorIfFound(model.InvitedAssociationByDonor, bid, model.IsAssociationFoundToSupervise, model.SupervisingAssociationId);
                        if (!res.IsSucceeded)
                            return OperationResult<AddBidResponse>.Fail(res.HttpErrorCode, res.Code);
                    }
                    if ((usr.UserType == UserType.SuperAdmin || usr.UserType == UserType.Admin) && bid.BidStatusId != (int)TenderStatus.Open && bid.BidStatusId != (int)TenderStatus.Draft && bid.BidStatusId != (int)TenderStatus.Reviewing)
                        return OperationResult<AddBidResponse>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);

                    //if (bid.BidStatusId != (int)TenderStatus.Draft)
                    //        return OperationResult<AddBidResponse>.Fail(HttpErrorCode.NotAuthorized, "you can edit bid when it is draft or rejected only.");
                    if (bid.BidStatusId != (int)TenderStatus.Rejected && bid.BidStatusId != (int)TenderStatus.Draft && (usr.UserType == UserType.Association || usr.UserType == UserType.Donor))
                        return OperationResult<AddBidResponse>.Fail(HttpErrorCode.NotAuthorized, BidErrorCodes.YOU_CAN_EDIT_BID_WHEN_IT_IS_DRAFT_OR_REJECTED_ONLY);

                    UpdateSiteMapLastModificationDateIfSpecificDataChanged(bid, model);
                    bidId = bid.Id;
                    bid.BidName = model.BidName;
                    bid.Objective = model.Objective;
                    if (await CheckIfWeCanUpdatePriceOfBid(usr, bid))
                    {
                        var calculationResult = CalculateAndUpdateBidPrices(model.Association_Fees, generalSettings, bid);
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

                    await ValidateInvitationAttachmentsAndUpdateThemNew(model, usr);

                    await this.UpdateBidRegions(model.RegionsId, bidId);

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
                            await UpdateBidStatus(bid.Id);

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
                        var res = await SaveBidDonor(model.DonorRequest, bid.Id, usr.Id);
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
                        await UpdateBidRelatedAttachmentsFileNameAfterBidNameChanging(bidId, model.BidName);

                    return OperationResult<AddBidResponse>.Success(new AddBidResponse { Id = bid.Id, Ref_Number = bid.Ref_Number, BidVisibility = (BidTypes)bid.BidTypeId });
                }
                else
                {

                    var validationOfBidDates = ValidateBidDates(model, null, generalSettings);
                    if (!validationOfBidDates.IsSucceeded)
                        return OperationResult<AddBidResponse>.Fail(validationOfBidDates.HttpErrorCode, validationOfBidDates.Code, validationOfBidDates.ErrorMessage);

                    var entity = _mapper.Map<Bid>(model);
                    if (ValidateBidInvitationAttachmentsNew(model))
                        return OperationResult<AddBidResponse>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.ADDING_INVITATION_ATTACHMENTS_REQUIRED);

                    var calculationResult = CalculateAndUpdateBidPrices(model.Association_Fees, generalSettings, entity);
                    if (!calculationResult.IsSucceeded)
                        return OperationResult<AddBidResponse>.Fail(calculationResult.HttpErrorCode, calculationResult.Code, calculationResult.ErrorMessage);
                    //generate code
                    string firstPart_Ref_Number = _dateTimeZone.CurrentDate.ToString("yy") + _dateTimeZone.CurrentDate.ToString("MM") + model.BidTypeId.ToString();
                    string randomNumber = await GenerateBidRefNumber(model.Id, firstPart_Ref_Number);

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
                        var res = await this.AddInvitationToAssocationByDonorIfFound(model.InvitedAssociationByDonor, entity, model.IsAssociationFoundToSupervise, model.SupervisingAssociationId);
                        if (!res.IsSucceeded)
                        {
                            await this._bidRepository.Delete(entity);
                            return OperationResult<AddBidResponse>.Fail(res.HttpErrorCode, res.Code);
                        }
                    }

                    bidId = entity.Id;

                    AddInvitationAttachmentsNew(model, usr, bidId);

                    await this.AddBidRegions(model.RegionsId, bidId);

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
                    await UpdateInvitationRequiredDocumentsEndDateNew(model.InvitationDocumentsApplyingEndDate, entity);

                    await _bidAddressesTimeRepository.Add(entityBidAddressesTime);
                    //    bidAddressesTimesId = entity.Id;
                    #endregion

                    if (model.IsFunded)
                    {
                        var res = await SaveBidDonor(model.DonorRequest, bidId, usr.Id);
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

        /// <summary>
        /// Nulls financial value when bid type is not Private or Public
        /// </summary>
        /// <param name="model"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void ValidateBidFinancialValueWithBidType(AddBidModelNew model)
        {
            if (model.BidTypeId != (int)BidTypes.Public && model.BidTypeId != (int)BidTypes.Private)
            {
                model.IsFinancialInsuranceRequired = false;
                model.BidFinancialInsuranceValue = null;
            }
        }

        private static bool IsRequiredDataForNotSaveAsDraftAdded(AddBidModelNew model)
        {
            var isAllRequiredDatesAdded = model.LastDateInReceivingEnquiries.HasValue &&
                 model.LastDateInOffersSubmission.HasValue && model.OffersOpeningDate.HasValue;
            return !model.IsDraft && ((!isAllRequiredDatesAdded) || (model.RegionsId is null || model.RegionsId.Count == 0));
        }

        private OperationResult<bool> AdjustRequestBidAddressesToTheEndOfTheDay<T>(T model) where T : BidAddressesModelRequest
        {
            if (model is null)
                return OperationResult<bool>.Fail(HttpErrorCode.InvalidInput, CommonErrorCodes.INVALID_INPUT);

            model.LastDateInReceivingEnquiries = model.LastDateInReceivingEnquiries is null ? model.LastDateInReceivingEnquiries : new DateTime(model.LastDateInReceivingEnquiries.Value.Year, model.LastDateInReceivingEnquiries.Value.Month, model.LastDateInReceivingEnquiries.Value.Day, 23, 59, 59);
            model.LastDateInOffersSubmission = model.LastDateInOffersSubmission is null ? model.LastDateInOffersSubmission : new DateTime(model.LastDateInOffersSubmission.Value.Year, model.LastDateInOffersSubmission.Value.Month, model.LastDateInOffersSubmission.Value.Day, 23, 59, 59);
            model.OffersOpeningDate = model.OffersOpeningDate is null ? model.OffersOpeningDate : new DateTime(model.OffersOpeningDate.Value.Year, model.OffersOpeningDate.Value.Month, model.OffersOpeningDate.Value.Day, 00, 00, 00);
            model.ExpectedAnchoringDate = model.ExpectedAnchoringDate.HasValue ? new DateTime(model.ExpectedAnchoringDate.Value.Year, model.ExpectedAnchoringDate.Value.Month, model.ExpectedAnchoringDate.Value.Day, 00, 00, 00) : null;

            return OperationResult<bool>.Success(true);
        }

        private OperationResult<AddBidResponse> ValidateBidDates(AddBidModelNew model, Bid bid, ReadOnlyAppGeneralSettings generalSettings)
        {
            if (bid is not null && checkLastReceivingEnqiryDate(model, bid))
                return OperationResult<AddBidResponse>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.LAST_DATE_IN_RECEIVING_ENQUIRIES_MUST_NOT_BE_BEFORE_TODAY_DATE);

            else if (model.LastDateInReceivingEnquiries > model.LastDateInOffersSubmission)
                return OperationResult<AddBidResponse>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.LAST_DATE_IN_OFFERS_SUBMISSION_MUST_BE_GREATER_THAN_LAST_DATE_IN_RECEIVING_ENQUIRIES);

            else if (model.LastDateInOffersSubmission > model.OffersOpeningDate)
                return OperationResult<AddBidResponse>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.OFFERS_OPENING_DATE_MUST_BE_GREATER_THAN_LAST_DATE_IN_OFFERS_SUBMISSION);

            else if (model.ExpectedAnchoringDate != null && model.ExpectedAnchoringDate != default
                && model.OffersOpeningDate.Value.AddDays(generalSettings.StoppingPeriodDays) > model.ExpectedAnchoringDate)
                return OperationResult<AddBidResponse>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.EXPECTED_ANCHORING_DATE_MUST_BE_GREATER_THAN_OFFERS_OPENING_DATE_PLUS_STOPPING_PERIOD);
            else
                return OperationResult<AddBidResponse>.Success(null);
        }

        private void UpdateSiteMapLastModificationDateIfSpecificDataChanged(Bid bid, AddBidModelNew requestModel)
        {
            if (bid is null || requestModel is null)
                return;

            if (bid.BidName != requestModel.BidName
                || bid.Objective != requestModel.Objective
                || bid.Bid_Documents_Price != requestModel.Bid_Documents_Price)
                bid.SiteMapDataLastModificationDate = _dateTimeZone.CurrentDate;
        }

        private OperationResult<bool> CalculateAndUpdateBidPrices(double association_Fees, ReadOnlyAppGeneralSettings settings, Bid bid)
        {
            if (bid is null || settings is null)
                return OperationResult<bool>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.NOT_FOUND);

            double tanafosMoneyWithoutTax = Math.Round((association_Fees * ((double)settings.TanfasPercentage / 100)), 8);
            if (tanafosMoneyWithoutTax < settings.MinTanfasOfBidDocumentPrice)
                tanafosMoneyWithoutTax = settings.MinTanfasOfBidDocumentPrice;

            var bidDocumentPricesWithoutTax = Math.Round((association_Fees + tanafosMoneyWithoutTax), 8);
            var bidDocumentTax = Math.Round((bidDocumentPricesWithoutTax * ((double)settings.VATPercentage / 100)), 8);
            var bidDocumentPricesWithTax = Math.Round((bidDocumentPricesWithoutTax + bidDocumentTax), 8);

            if (association_Fees < 0 || bidDocumentPricesWithTax > settings.MaxBidDocumentPrice)
                return OperationResult<bool>.Fail(HttpErrorCode.Conflict, CommonErrorCodes.INVALID_INPUT);

            bid.Association_Fees = association_Fees;
            bid.Tanafos_Fees = tanafosMoneyWithoutTax;
            bid.Bid_Documents_Price = bidDocumentPricesWithTax;

            return OperationResult<bool>.Success(true);
        }

        private async Task<bool> IsTermsBookBoughtBeforeInBid(long bidId)
        {
            var isBoughtBefore = await _providerBidRepository.Any(x => x.BidId == bidId && x.IsPaymentConfirmed);
            return isBoughtBefore;
        }


        private async Task<bool> CheckIfWeCanUpdatePriceOfBid(ApplicationUser usr, Bid bid)
        {

            if (bid.BidStatusId == (int)TenderStatus.Draft)
                return true;

            if (
                (usr.UserType == UserType.SuperAdmin || usr.UserType == UserType.Admin)
                &&
                (
                (bid.BidStatusId == (int)TenderStatus.Reviewing)
                ||
                 (bid.BidStatusId == (int)TenderStatus.Open && !(await IsTermsBookBoughtBeforeInBid(bid.Id)))
                )
              )
                return true;

            return false;
        }



        private bool checkLastReceivingEnqiryDate(AddBidModelNew model, Bid bid)
        {
            return bid.BidAddressesTime is not null && bid.BidAddressesTime.LastDateInReceivingEnquiries.HasValue &&
                bid.BidAddressesTime.LastDateInReceivingEnquiries.Value.Date != model.LastDateInReceivingEnquiries.Value.Date &&
                                    model.LastDateInReceivingEnquiries < _dateTimeZone.CurrentDate.Date;
        }

        private async Task<OperationResult<bool>> SaveBidDonor(BidDonorRequest model, long bidId, string UserId)
        {
            if (model is null)
                return OperationResult<bool>.Fail(HttpErrorCode.InvalidInput, CommonErrorCodes.INVALID_INPUT);

            BidDonor oldBidDonor = await _BidDonorRepository.FindOneAsync(a => a.Id == model.BidDonorId);

            //===================check validation=============================

            if (model.DonorId == 0 && (String.IsNullOrEmpty(model.NewDonorName) && String.IsNullOrEmpty(model.Email)
                                && String.IsNullOrEmpty(model.PhoneNumber)))
            {
                if (oldBidDonor is null)
                    return OperationResult<bool>.Fail(HttpErrorCode.InvalidInput, CommonErrorCodes.INVALID_INPUT); //fist insert

                else
                    await _BidDonorRepository.Delete(oldBidDonor);  //delete
            }
            //================Insert on BidDonor===================
            if (oldBidDonor is null) //|| oldBidDonor.DonorResponse == DonorResponse.Reject)
            {
                await _BidDonorRepository.Add(new BidDonor
                {
                    BidId = bidId,
                    DonorId = model.DonorId == 0 ? null : model.DonorId,
                    NewDonorName = model.NewDonorName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    DonorResponse = DonorResponse.NotReviewed,
                    CreationDate = _dateTimeZone.CurrentDate,
                    CreatedBy = UserId
                });
            }
            //================update on BidDonor===================
            else
            {
                oldBidDonor.DonorId = model.DonorId == 0 ? null : model.DonorId;
                oldBidDonor.NewDonorName = model.NewDonorName;
                oldBidDonor.Email = model.Email;
                oldBidDonor.PhoneNumber = model.PhoneNumber;
                oldBidDonor.ModificationDate = _dateTimeZone.CurrentDate;
                oldBidDonor.ModifiedBy = UserId;
                await _BidDonorRepository.Update(oldBidDonor);
            }
            return OperationResult<bool>.Success(true);

        }

        public async Task SendEmailAndNotifyDonor(Bid bid)
        {
            BidDonor bidDonor = await _BidDonorRepository
                .Find(a => a.BidId == bid.Id && !a.IsEmailSent && a.DonorResponse != DonorResponse.Reject, false, nameof(BidDonor.Donor))
                .OrderByDescending(a => a.CreationDate)
                .FirstOrDefaultAsync();

            if (bidDonor is null)
                return;

            if (bidDonor.DonorId.HasValue)
            {
                //================Send Email===================

                var emailModel = new PublishBidDonorEmail()
                {
                    BaseBidEmailDto = await _helperService.GetBaseDataForBidsEmails(bid)
                };

                var emailRequest = new EmailRequest()
                {
                    ControllerName = BaseBidEmailDto.BidsEmailsPath,
                    ViewName = PublishBidDonorEmail.EmailTemplateName,
                    ViewObject = emailModel,
                    To = await _donorService.GetEmailOfUserSelectedToReceiveEmails(bidDonor.DonorId ?? 0, bidDonor.Donor.ManagerEmail),  //bidDonor.Donor is null ? bidDonor.Email : bidDonor.Donor.ManagerEmail,
                    Subject = $"طرح منافسة ممنوحة من قبل {emailModel.BaseBidEmailDto.EntityName}",
                    SystemEventType = (int)SystemEventsTypes.PublishBidDonorEmail
                };
                await _emailService.SendAsync(emailRequest);

                //================send Notifications===================
                var donorUsers = await _notificationUserClaim.GetUsersClaim(new string[] { DonorClaimCodes.clm_3047.ToString() }, bidDonor.Donor.Id, OrganizationType.Donor);
                var assocUser = await _userManager.FindByEmailAsyncSafe(bid.Association?.Manager_Email);
                var _notificationService = (INotificationService)_serviceProvider.GetService(typeof(INotificationService));

                var notificationObj = await _notificationService.GetNotificationObjModelToSendAsync(new NotificationObjModel
                {
                    EntityId = bid.Id,
                    Message = $"تم طرح منافسة {bid.BidName} الممنوحة من قبلكم، اسم الجهة: {bid.Association.Association_Name}",
                    ActualRecieverIds = donorUsers.ActualReceivers,
                    SenderId = assocUser?.Id,
                    NotificationType = NotificationType.DonorSupervisingBid,
                    ServiceType = ServiceType.Bids,
                    SystemEventType = (int)SystemEventsTypes.PublishBidRequestNotification

                });

                notificationObj.BidId = bid.Id;
                notificationObj.BidName = bid.BidName;
                notificationObj.SenderName = await GetBidCreatorName(bid);
                notificationObj.AssociationName = notificationObj.SenderName;

                await _notificationService.SendRealTimeNotificationToUsersAsync(notificationObj, donorUsers.RealtimeReceivers.Select(a => a.ActualRecieverId).ToList(), (int)SystemEventsTypes.PublishBidRequestNotification);
            }
            else
            {
                //=================update on bid==========================فى حالة لو المانح مدعو وليس مسجل

                var invitieDonorEmailModel = new InviteSupervisorDonorOfBidToTanafosEmailModel()
                {
                    BaseBidEmailDto = await _helperService.GetBaseDataForBidsEmails(bid),
                    AssociationName = bid.Association?.Association_Name,
                    BidName = bid.BidName,
                    RecieverName = bidDonor.NewDonorName,
                    DonorSignupURL = $"{fileSettings.ONLINE_URL}{FrontendUrls.GetSignupURLForSpecificUserType(UserType.Donor)}",
                };

                var emailRequest = new EmailRequest
                {
                    ControllerName = BaseBidEmailDto.BidsEmailsPath,
                    ViewName = InviteSupervisorDonorOfBidToTanafosEmailModel.EmailTemplateName,
                    ViewObject = invitieDonorEmailModel,
                    To = bidDonor.Email,
                    Subject = "دعوة للتسجيل بمنصة تنافس",
                    SystemEventType = (int)SystemEventsTypes.InviteSupervisorDonorOfBidToTanafosEmail,
                };
                await _emailService.SendAsync(emailRequest);

                bid.BidDonorId = bidDonor.Id;
                await _bidRepository.Update(bid);
            }

            //=================update on bid donnor==========================
            bidDonor.IsEmailSent = true;
            bidDonor.InvitationDate = _dateTimeZone.CurrentDate;
            await _BidDonorRepository.Update(bidDonor);
        }

        private async Task SendNewBidEmailToSuperAdmins(Bid bid)
        {
            if (bid is null)
                throw new ArgumentNullException("bid is null");


            var superAdminsEmails = await _userManager.Users
                .Where(x => x.UserType == UserType.SuperAdmin)
                .Select(a => a.Email)
                .ToListAsync();
            var _commonEmailAndNotificationService = (ICommonEmailAndNotificationService)_serviceProvider.GetService(typeof(ICommonEmailAndNotificationService));

            var adminPermissionUsers = await _commonEmailAndNotificationService.GetAdminClaimOfEmails(new List<AdminClaimCodes> { AdminClaimCodes.clm_2553 });
            superAdminsEmails.AddRange(adminPermissionUsers);

            var bidIndustriesAsString = string.Join(',', bid.GetBidWorkingSectors().Select(x => x.NameAr));
            var body = string.Empty;

            var lastDateInOffersSubmission = await _bidAddressesTimeRepository
                .Find(a => a.BidId == bid.Id)
                .Select(x => x.LastDateInOffersSubmission)
                .FirstOrDefaultAsync();

            var emailModel = new NewBidToSuperAdminEmail()
            {
                BaseBidEmailDto = await _helperService.GetBaseDataForBidsEmails(bid),
                Industies = bidIndustriesAsString,
                ClosingOffersDateTime = lastDateInOffersSubmission?.ToArabicFormatWithTime()
            };
            var emailRequest = new EmailRequestMultipleRecipients()
            {
                ControllerName = BaseBidEmailDto.BidsEmailsPath,
                ViewName = NewBidToSuperAdminEmail.EmailTemplateName,
                ViewObject = emailModel,
                Recipients = superAdminsEmails.Select(s => new RecipientsUser { Email = s }).ToList(),
                Subject = $"إنشاء منافسة جديدة {bid.BidName}",
                SystemEventType = (int)SystemEventsTypes.NewBidToSuperAdminEmail
            };
            await _emailService.SendToMultipleReceiversAsync(emailRequest);
        }
        private async Task SendNewDraftBidEmailToSuperAdmins(Bid bid, string entityName)
        {
            if (bid is null)
                throw new ArgumentNullException("bid is null");

            using var scope = _serviceScopeFactory.CreateScope();
            var bidRepo = scope.ServiceProvider.GetRequiredService<ICrossCuttingRepository<Bid, long>>();
            var _commonEmailAndNotificationService = scope.ServiceProvider.GetRequiredService<ICommonEmailAndNotificationService>();

            var superAdminsEmails = await _userManager.Users
                .Where(x => x.UserType == UserType.SuperAdmin)
                .Select(a => a.Email)
                .ToListAsync();

            var adminPermissionUsers = await _commonEmailAndNotificationService.GetAdminClaimOfEmails(new List<AdminClaimCodes> { AdminClaimCodes.clm_2553 });
            superAdminsEmails.AddRange(adminPermissionUsers);

            var bidInDb = await bidRepo.Find(x => x.Id == bid.Id)
                .IncludeBasicBidData()
                .FirstOrDefaultAsync();

            var emailModel = new NewDraftAddedToAdminsEmail()
            {
                BaseBidEmailDto = await _helperService.GetBaseDataForBidsEmails(bidInDb)
            };
            var emailRequest = new EmailRequestMultipleRecipients()
            {
                ControllerName = BaseBidEmailDto.BidsEmailsPath,
                ViewName = NewDraftAddedToAdminsEmail.EmailTemplateName,
                ViewObject = emailModel,
                Recipients = superAdminsEmails.Select(s => new RecipientsUser { Email = s }).ToList(),
                Subject = $"طرح مسودة منافسة {bid.BidName}",
                SystemEventType = (int)SystemEventsTypes.NewDraftAddedToAdminsEmail
            };
            await _emailService.SendToMultipleReceiversAsync(emailRequest);
        }
        private async Task<string> GenerateBidRefNumber(long bidId, string firstPart_Ref_Number)
        {
            string randomNumber = _randomGeneratorService.RandomNumber(1000, 9999);// + _randomGeneratorService.RandomString(3, true);
            string ref_Number = firstPart_Ref_Number + randomNumber;
            var checkBid = await _bidRepository.FindOneAsync(x =>
                          (string.Equals(x.Ref_Number.ToLower(), ref_Number)) && x.Id != bidId);
            do
            {
                randomNumber = _randomGeneratorService.RandomNumber(1000, 9999);// + _randomGeneratorService.RandomString(3, true);
                ref_Number = firstPart_Ref_Number + randomNumber;
                checkBid = await _bidRepository.FindOneAsync(x =>
                               (string.Equals(x.Ref_Number.ToLower(), ref_Number)) && x.Id != bidId);

            } while (checkBid != null);
            return ref_Number;
        }

        private static bool ValidateBidInvitationAttachmentsNew(AddBidModelNew model)
        {
            return model.BidVisibility == BidTypes.Habilitation &&
                (model.IsInvitationNeedAttachments.HasValue ? model.IsInvitationNeedAttachments.Value : false)
                && (model.BidInvitationsAttachments is null || model.BidInvitationsAttachments.Count == 0);
        }
        private void AddInvitationAttachmentsNew(AddBidModelNew model, ApplicationUser usr, long bidId)
        {
            if (checkIfWeNeedAddAttachmentNew(model))
            {
                var attachments = _mapper.Map<List<AddBidInvitationAttachmentModel>, List<InvitationRequiredDocument>>(model.BidInvitationsAttachments);
                attachments.ForEach(attachment =>
                {
                    attachment.BidId = bidId;
                    attachment.IsDeleted = false;
                    attachment.CreatedBy = usr.Id;
                    attachment.CreationDate = _dateTimeZone.CurrentDate;

                });
                _invitationRequiredDocumentRepository.AddRange(attachments);

            };
        }
        private static bool checkIfWeNeedAddAttachmentNew(AddBidModelNew model)
        {
            return model.BidVisibility == BidTypes.Habilitation &&
                model.IsInvitationNeedAttachments == true && model.BidInvitationsAttachments != null
                && model.BidInvitationsAttachments.Count > 0;
        }



        private async Task<(List<NotificationReceiverUser> ActualReceivers, List<NotificationReceiverUser> RealtimeReceivers)> GetUsersOfBidCreatorOrganizationToRecieveBidNotifications(Bid bid)
        {
            string[] claims = null;
            long entityId = 0;
            var organizationType = OrganizationType.Assosition;

            if (bid.EntityType == UserType.Association)
            {
                claims = new string[] { AssociationClaimCodes.clm_3030.ToString(), AssociationClaimCodes.clm_3031.ToString(), AssociationClaimCodes.clm_3032.ToString(), AssociationClaimCodes.clm_3033.ToString() };
                entityId = bid.AssociationId.Value;
                organizationType = OrganizationType.Assosition;
            }
            else
            {
                claims = new string[] { DonorClaimCodes.clm_3047.ToString(), DonorClaimCodes.clm_3048.ToString(), DonorClaimCodes.clm_3049.ToString(), DonorClaimCodes.clm_3050.ToString() };
                entityId = bid.DonorId.Value;
                organizationType = OrganizationType.Donor;
            }

            return await _notificationUserClaim.GetUsersClaim(claims, entityId, organizationType);
        }


        public async Task SendUpdatedBidEmailToCreatorAndProvidersOfThisBid(Bid bid)
        {
            var emailsToSend = new List<RecipientsUser>();

            var emailOfBidCreator = await GetBidCreatorEmailToReceiveEmails(bid);
            emailsToSend.Add(new RecipientsUser { Email = emailOfBidCreator });

            var usersToRevieveNotification = await GetUsersOfBidCreatorOrganizationToRecieveBidNotifications(bid);

            if (bid.BidStatusId != (int)TenderStatus.Draft && bid.BidStatusId != (int)TenderStatus.Reviewing)
            {
                var ParticipantEmails = (await _helperService.GetBidTermsBookBuyersDataAsync(bid))
                    .Select(x => new RecipientsUser() { Email = x.EntityEmail }).ToList();
                emailsToSend.AddRange(ParticipantEmails);

                var participantNotificationRecievers = await GetProvidersUserIdsWhoBoughtTermsPolicyForNotification(bid);
                usersToRevieveNotification.RealtimeReceivers.AddRange(participantNotificationRecievers.RealtimeReceivers);
                usersToRevieveNotification.ActualReceivers.AddRange(participantNotificationRecievers.ActualReceivers);
            }


            var emailModel = new UpdateOnBidEmail()
            {
                BaseBidEmailDto = await _helperService.GetBaseDataForBidsEmails(bid)
            };
            var emailRequest = new EmailRequestMultipleRecipients
            {
                ControllerName = BaseBidEmailDto.BidsEmailsPath,
                ViewName = UpdateOnBidEmail.EmailTemplateName,
                ViewObject = emailModel,
                Subject = $"تحديث على منافسة {bid.BidName}",
                Recipients = emailsToSend,
                SystemEventType = (int)SystemEventsTypes.UpdateOnBidEmail
            };

            await _emailService.SendToMultipleReceiversAsync(emailRequest);

            if (usersToRevieveNotification.ActualReceivers.Count > 0)
            {
                var _notificationService = (INotificationService)_serviceProvider.GetService(typeof(INotificationService));

                var notificationObj = await _notificationService.GetNotificationObjModelToSendAsync(new NotificationObjModel
                {
                    EntityId = bid.Id,
                    Message = $"تم تحديث منافستكم {bid.BidName}",
                    ActualRecieverIds = usersToRevieveNotification.ActualReceivers,
                    SenderId = _currentUserService.CurrentUser.Id,
                    NotificationType = NotificationType.BidGotUpdatedByAdmins,
                    ServiceType = ServiceType.Bids
                    ,
                    SystemEventType = (int)SystemEventsTypes.UpdateBidNotification
                });

                notificationObj.BidName = bid.BidName;
                notificationObj.BidId = bid.Id;

                await _notificationService.SendRealTimeNotificationToUsersAsync(notificationObj, usersToRevieveNotification.RealtimeReceivers.Select(a => a.ActualRecieverId).ToList(), (int)SystemEventsTypes.UpdateBidNotification);
            }
        }


        private async Task ValidateInvitationAttachmentsAndUpdateThemNew(AddBidModelNew model, ApplicationUser usr)
        {
            if (!(model.IsInvitationNeedAttachments.HasValue ? model.IsInvitationNeedAttachments.Value : false))
            {
                var invitationRequiredDocs = await _invitationRequiredDocumentRepository.Find(x => x.BidId == model.Id).ToListAsync();
                await _invitationRequiredDocumentRepository.DeleteRangeAsync(invitationRequiredDocs);

            }

            if (ValidateIfWeNeedToUpdateInvitationAttachmentsNew(model))
            {
                var invitationRequiredDocs = await _invitationRequiredDocumentRepository.Find(x => x.BidId == model.Id).ToListAsync();
                await _invitationRequiredDocumentRepository.DeleteRangeAsync(invitationRequiredDocs);
                AddInvitationAttachmentsNew(model, usr, model.Id);
            }
        }

        private static bool ValidateIfWeNeedToUpdateInvitationAttachmentsNew(AddBidModelNew model)
        {
            return model.IsInvitationNeedAttachments.HasValue ? model.IsInvitationNeedAttachments.Value : false
                && model.BidInvitationsAttachments != null && model.BidInvitationsAttachments.Count > 0;
        }



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
                    await UpdateInvitationRequiredDocumentsEndDate(model, bid);

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

        private async Task UpdateInvitationRequiredDocumentsEndDate(AddBidAddressesTimesModel model, Bid bid)
        {
            if (bid.BidTypeId == (int)BidTypes.Habilitation && model.InvitationDocumentsApplyingEndDate != null)
            {

                bid.InvitationDocumentsApplyingEndDate = model.InvitationDocumentsApplyingEndDate;
                await _bidRepository.Update(bid);

            }
        }
        private async Task UpdateInvitationRequiredDocumentsEndDateNew(DateTime? InvitationDocumentsApplyingEndDate, Bid bid)
        {
            if (bid.BidTypeId == (int)BidTypes.Habilitation && InvitationDocumentsApplyingEndDate != null)
            {

                bid.InvitationDocumentsApplyingEndDate = InvitationDocumentsApplyingEndDate;
                await _bidRepository.Update(bid);

            }
        }

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
                    donor = await GetDonorUser(usr);
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
                            Text = "(تنويه هام) نلفت عنايتكم إلى أنه قد تم إجراء بعض التغييرات في جدول الكميات، نرجو منكم إعادة إرسال عروضكم بناء على هذا التغيير"
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
        public async Task ExecutePostPublishingLogic(Bid bid, ApplicationUser usr, TenderStatus oldStatusOfBid)
        {
            if (oldStatusOfBid != TenderStatus.Draft || bid.BidStatusId != (int)TenderStatus.Open)
                return;

            await ApplyDefaultFlowOfApproveBid(usr, bid);
        }

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
                    var donor = await GetDonorUser(usr);
                    if (donor == null)
                        return OperationResult<AddBidAttachmentsResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.DONOR_NOT_FOUND);

                    if (bid.DonorId != donor.Id)
                        return OperationResult<AddBidAttachmentsResponse>.Fail(HttpErrorCode.InvalidInput, CommonErrorCodes.INVALID_INPUT);
                }

                var checkQuantitesTableForThisBid = await _bidQuantitiesTableRepository.Find(a => a.BidId == bid.Id).AnyAsync();
                if (!checkQuantitesTableForThisBid)
                    return OperationResult<AddBidAttachmentsResponse>.Fail(HttpErrorCode.Conflict, CommonErrorCodes.PLEASE_FILL_ALL_REQUIRED_DATA_IN_PREVIOUS_STEPS);

                var oldStatusOfBid = (TenderStatus)bid.BidStatusId;

                string imagePath = !string.IsNullOrEmpty(model.Tender_Brochure_Policies_Url)?  _encryptionService.Decrypt(model.Tender_Brochure_Policies_Url):null;

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
                if (model.BidStatusId.HasValue && CheckIfWasDraftAndChanged(model.BidStatusId.Value, oldStatusOfBid) && !bid.CanPublishBid())
                    return OperationResult<AddBidAttachmentsResponse>.Fail(HttpErrorCode.InvalidInput, CommonErrorCodes.PLEASE_FILL_ALL_REQUIRED_DATA_IN_PREVIOUS_STEPS);

                List<BidAttachment> bidAttachmentsToSave = await SaveBidAttachments(model, bid);
                var supervisingDonorClaims = await _donorService.GetFundedDonorSupervisingServiceClaims(bid.Id);
                //if (CheckIfAdminCanPublishBid(usr, bid))
                //    await ApplyClosedBidsLogicIfAdminTryToPublish(model, usr, bid, oldStatusOfBid);
                if (bid.BidTypeId != (int)BidTypes.Private)
                    await ApplyClosedBidsLogic(model, usr, bid, supervisingDonorClaims);

                else
                    await _bidRepository.Update(bid);
                if (!CheckIfHasSupervisor(bid, supervisingDonorClaims) && CheckIfWeShouldSendPublishBidRequestToAdmins(bid, oldStatusOfBid))
                    await SendPublishBidRequestEmailAndNotification(usr, bid, oldStatusOfBid);
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
                        await AddSystemReviewToBidByCurrentUser(bid.Id, SystemRequestStatuses.Accepted);

                        await ExecutePostPublishingLogic(bid, usr, oldStatusOfBid);
                    }

                    if (model.IsSendEmailsAndNotificationAboutUpdatesChecked)
                        await SendUpdatedBidEmailToCreatorAndProvidersOfThisBid(bid);
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

        private async Task<OperationResult<bool>> AddSystemReviewToBidByCurrentUser(long bidId, SystemRequestStatuses status)
            => await _helperService.AddReviewedSystemRequestLog(
                new AddReviewedSystemRequestLogRequest
                {
                    EntityId = bidId,
                    SystemRequestStatus = status,
                    SystemRequestType = SystemRequestTypes.BidReviewing,
                    Note = null

                }, _currentUserService.CurrentUser);

        private static bool CheckIfHasSupervisor(Bid bid, OperationResult<List<GetDonorSupervisingServiceClaimsResponse>> supervisingDonorClaims)
        {
            return bid.IsFunded && bid.BidStatusId != (int)TenderStatus.Draft &&
                supervisingDonorClaims.Data.Any(x => x.ClaimType == SupervisingServiceClaimCodes.clm_3057 && x.IsChecked);
        }

        private async Task SendPublishBidRequestEmailAndNotification(ApplicationUser usr, Bid bid, TenderStatus oldStatusOfBid)
        {
            var emailModel = new PublishBidRequestEmail()
            {
                BaseBidEmailDto = await _helperService.GetBaseDataForBidsEmails(bid),
                BidIndustries = string.Join(',', bid.GetBidWorkingSectors().Select(i => i.NameAr)),
            };

            var (adminEmails, adminUsers) = await _notificationUserClaim.GetEmailsAndUserIdsOfSuperAdminAndAuthorizedAdmins(new List<string>() { AdminClaimCodes.clm_2553.ToString() });
            var emailRequest = new EmailRequestMultipleRecipients
            {
                ControllerName = BaseBidEmailDto.BidsEmailsPath,
                ViewName = PublishBidRequestEmail.EmailTemplateName,
                ViewObject = emailModel,
                Subject = $"طلب إنشاء منافسة جديدة {bid.BidName} بانتظار مراجعتكم",
                Recipients = adminEmails.Select(s => new RecipientsUser { Email = s }).ToList(),
                SystemEventType = (int)SystemEventsTypes.PublishBidRequestEmail
            };
            var _notificationService = (INotificationService)_serviceProvider.GetService(typeof(INotificationService));
            var notificationObj = await _notificationService.GetNotificationObjModelToSendAsync(new NotificationObjModel
            {
                EntityId = bid.Id,
                Message = $"طلب إنشاء منافسة جديدة  {bid.BidName} بانتظار مراجعتكم",
                ActualRecieverIds = adminUsers,
                SenderId = usr.Id,
                NotificationType = NotificationType.PublishBidRequest,
                ServiceType = ServiceType.Bids
                ,
                SystemEventType = (int)SystemEventsTypes.PublishBidRequestNotification
            });
            await _emailService.SendToMultipleReceiversAsync(emailRequest);

            notificationObj.BidId = bid.Id;
            notificationObj.BidName = bid.BidName;
            notificationObj.EntityId = bid.Id;
            notificationObj.SenderName = emailModel.BaseBidEmailDto.EntityName;
            notificationObj.AssociationName = emailModel.BaseBidEmailDto.EntityName;

            await _notificationService.SendRealTimeNotificationToUsersAsync(notificationObj, adminUsers.Select(x => x.ActualRecieverId).ToList(), (int)SystemEventsTypes.PublishBidRequestNotification);
        }

        private static bool CheckIfWeShouldSendPublishBidRequestToAdmins(Bid bid, TenderStatus oldStatusOfBid)
        {
            return oldStatusOfBid == TenderStatus.Draft && (TenderStatus)bid.BidStatusId == TenderStatus.Reviewing && bid.BidTypeId != (int)BidTypes.Private;
        }

        private static bool CheckIfAdminCanPublishBid(ApplicationUser usr, Bid bid)
        {
            return (usr.UserType == UserType.SuperAdmin || usr.UserType == UserType.Admin) && bid.BidTypeId != (int)BidTypes.Private;
        }

        private async Task ApplyClosedBidsLogicIfAdminTryToPublish(AddBidAttachmentRequest model, ApplicationUser usr, Bid bid, TenderStatus oldStatusOfBid)
        {
            bid.BidStatusId = model.BidStatusId != null && model.BidStatusId > 0 ? Convert.ToInt32(model.BidStatusId) : (int)TenderStatus.Open;
            if (CheckIfWasDraftAndBecomeOpen(bid, oldStatusOfBid))
                bid.CreationDate = _dateTimeZone.CurrentDate;
            else
                bid.CreationDate = bid.CreationDate;

            var bidDonor = await _donorService.GetBidDonorOfBidIfFound(bid.Id);
            var supervisingDonorClaims = await _donorService.GetFundedDonorSupervisingServiceClaims(bid.Id);

            var canWePublishBid = CheckIfWeCanPublishBid(bid, oldStatusOfBid, bidDonor, supervisingDonorClaims);
            if (canWePublishBid)
            {

                await DoBusinessAfterPublishingBid(bid, usr);
                await _pointEventService.AddPointEventUsageHistoryAsync(new AddPointEventUsageHistoryModel
                {
                    PointType = PointTypes.PublishNonDraftBid,
                    ActionId = bid.Id,
                    EntityId = bid.AssociationId.HasValue ? bid.AssociationId.Value : bid.DonorId.Value,
                    EntityUserType = bid.AssociationId.HasValue ? UserType.Association : UserType.Donor,
                });

                if (CheckIfWasDraftAndBecomeOpen(bid, oldStatusOfBid))
                    await LogBidCreationEvent(bid);

                var addReviewedSystemRequestResult = await AddSystemReviewToBidByCurrentUser(bid.Id, SystemRequestStatuses.Accepted);

            }
            else if (bid.IsFunded && model.BidStatusId != (int)TenderStatus.Draft && supervisingDonorClaims.Data.Any(x => x.ClaimType == SupervisingServiceClaimCodes.clm_3057 && x.IsChecked))
            {
                await SendBidToSponsorDonorToBeConfirmed(usr, bid, bidDonor);
            }

            if (model.TenderBrochurePoliciesType == TenderBrochurePoliciesType.UsingRFP
                && CheckIfWasDraftAndBecomeOpen(bid, oldStatusOfBid))
                await SaveRFPAsPdf(bid);


            await _bidRepository.Update(bid);

        }
        private async Task ApplyClosedBidsLogic(AddBidAttachmentRequest model, ApplicationUser usr, Bid bid, OperationResult<List<GetDonorSupervisingServiceClaimsResponse>> supervisingDonorClaims)
        {

            bid.BidStatusId = model.BidStatusId != null && model.BidStatusId > 0 ? Convert.ToInt32(model.BidStatusId) : (int)TenderStatus.Reviewing;
            //bid.BidStatusId = bid.BidStatusId != (int)TenderStatus.Draft ?
            //                    (int)TenderStatus.Reviewing : bid.BidStatusId;



            var bidDonor = await _donorService.GetBidDonorOfBidIfFound(bid.Id);



            if (bid.IsFunded && model.BidStatusId != (int)TenderStatus.Draft && supervisingDonorClaims.Data.Any(x => x.ClaimType == SupervisingServiceClaimCodes.clm_3057 && x.IsChecked))
            {
                await SendBidToSponsorDonorToBeConfirmed(usr, bid, bidDonor);
                return;
            }


            await _bidRepository.Update(bid);

        }

        private async Task SendBidToSponsorDonorToBeConfirmed(ApplicationUser usr, Bid bid, BidDonor bidDonor)
        {
            bid.BidStatusId = (int)TenderStatus.Pending;

            var supervisingData = new BidSupervisingData
            {
                DonorId = bidDonor.DonorId.Value,
                CreatedBy = usr.Id,
                BidId = bid.Id,
                CreationDate = _dateTimeZone.CurrentDate,
                SupervisorStatus = SponsorSupervisingStatus.Pending,
                SupervisingServiceClaimCode = SupervisingServiceClaimCodes.clm_3057
            };
            await _bidSupervisingDataRepository.Add(supervisingData);
            var _commonEmailAndNotificationService = (ICommonEmailAndNotificationService)_serviceProvider.GetService(typeof(ICommonEmailAndNotificationService));
            await _commonEmailAndNotificationService.SendEmailAndNotifySupervisingDonorThatBisSubmissionWaitingHisAccept(bid, bidDonor.Donor);
            await _bidRepository.Update(bid);
        }

        public async Task<OperationResult<bool>> TakeActionOnPublishingBidByAdmin(PublishBidDto request)
        {
            try
            {

                var user = _currentUserService.CurrentUser;
                if (_currentUserService.IsUserNotAuthorized(new List<UserType>() { UserType.Admin, UserType.SuperAdmin }))
                    return OperationResult<bool>.Fail(HttpErrorCode.NotAuthorized, CommonErrorCodes.NotAuthorized);

                var bid = await _bidRepository.Find(x => x.Id == request.BidId && x.BidStatusId == (int)TenderStatus.Reviewing)
                        .Include(a => a.BidSupervisingData)
                        .IncludeBasicBidData()
                        .FirstOrDefaultAsync();
                if (bid is null)
                    return OperationResult<bool>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);

                if (request.IsApproved)
                {
                    if (bid.BidTypeId != (int)BidTypes.Instant && bid.BidTypeId != (int)BidTypes.Freelancing)
                    {
                        var generalSettingsResult = await _appGeneralSettingService.GetAppGeneralSettings();
                        if (!generalSettingsResult.IsSucceeded)
                            return OperationResult<bool>.Fail(generalSettingsResult.HttpErrorCode, generalSettingsResult.Code);
                        var generalSettings = generalSettingsResult.Data;

                        var validationOfBidDates = ValidateBidDatesWhileApproving(bid, generalSettings);
                        if (!validationOfBidDates.IsSucceeded)
                            return OperationResult<bool>.Fail(validationOfBidDates.HttpErrorCode, validationOfBidDates.Code, validationOfBidDates.ErrorMessage);
                    }
                    await AcceptPublishBid(user, bid, request.IsApplyOfferWithSubscriptionMandatory);
                }
                else
                    await RejectPublishBid(request.Notes, user, bid, request.IsApplyOfferWithSubscriptionMandatory);



                return OperationResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = new { request },
                    ErrorMessage = "Failed to Take Action On Publishing Bid By Admin!",
                    ControllerAndAction = "BidController/take-action-on-publishing-bid-by-admin"
                });
                return OperationResult<bool>.Fail(HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }

        }

        private async Task RejectPublishBid(string notes, ApplicationUser user, Bid bid, bool? isApplyOfferWithSubscriptionMandatory)
        {
            bid.BidStatusId = (int)TenderStatus.Draft;
            bid.IsApplyOfferWithSubscriptionMandatory = isApplyOfferWithSubscriptionMandatory;
            await _bidRepository.ExexuteAsTransaction(async () =>
            {

                await _helperService.AddReviewedSystemRequestLog(new AddReviewedSystemRequestLogRequest()
                {
                    EntityId = bid.Id,
                    RejectionReason = notes,
                    SystemRequestStatus = SystemRequestStatuses.Rejected,
                    SystemRequestType = SystemRequestTypes.BidReviewing,

                }, user);
                await _bidRepository.Update(bid);
            });

            await SendAdminRejectedBidEmail(notes, bid);
            SendAdminRejectBidNotification(bid);
        }

        private async Task SendAdminRejectedBidEmail(string notes, Bid bid)
        {
            var contactUs = await _appGeneralSettingService.GetContactUsInfoAsync();
            var emailModel = new RejectBidByAdminEmail()
            {

                BaseBidEmailDto = await _helperService.GetBaseDataForBidsEmails(bid),
                RejectionReason = notes,
                ContactUsEmailTo = contactUs.ContactUsEmailTo,
                ContactUsMobile = contactUs.ContactUsMobile
            };
            var emailRequest = new EmailRequest()
            {
                ControllerName = BaseBidEmailDto.BidsEmailsPath,
                ViewName = RejectBidByAdminEmail.EmailTemplateName,
                ViewObject = emailModel,
                Subject = $" رفض اعتماد منافستكم {bid.BidName}",
                SystemEventType = (int)SystemEventsTypes.RejectBidByAdminEmail

            };
            _notifyInBackgroundService.SendEmailInBackground(
                new SendEmailInBackgroundModel()
                {
                    EmailRequests = new List<ReadonlyEmailRequestModel>()
                    {
                           new ReadonlyEmailRequestModel()
                           {
                               EntityId=bid.EntityId,
                               EntityType=bid.EntityType,
                               EmailRequest=emailRequest
                           }
                    }
                });
        }

        private void SendAdminRejectBidNotification(Bid bid)
        {
            var notificationModel = new NotificationModel
            {
                BidId = bid.Id,
                BidName = bid.BidName,
                SenderName = null,

                EntityId = bid.Id,
                Message = $"تم رفض اعتماد منافستكم {bid.BidName}",
                NotificationType = NotificationType.BidReviewRejected,
                SenderId = _currentUserService.CurrentUser.Id,
                ServiceType = ServiceType.Bids
            };
            var notifyByNotification = new List<SendNotificationInBackgroundModel>()
                    {
                        new SendNotificationInBackgroundModel
                        {
                            IsSendToMultipleReceivers = true,
                            NotificationModel=notificationModel,
                            ClaimsThatUsersMustHaveToReceiveNotification= new List<string>(){ DonorClaimCodes.clm_3053.ToString(),AssociationClaimCodes.clm_3036.ToString() },

                            ReceiversOrganizations=new List<(long EntityId, OrganizationType EntityType)>()
                            {(bid.EntityId,bid.EntityType==UserType.Association?
                            OrganizationType.Assosition:OrganizationType.Donor)}

                        }
                    };
            _notifyInBackgroundService.SendNotificationInBackground(notifyByNotification);
        }

        private async Task AcceptPublishBid(ApplicationUser user, Bid bid, bool? IsSubscriptionMandatory)
        {
            bid.BidStatusId = (int)TenderStatus.Open;

            bid.CreationDate = _dateTimeZone.CurrentDate;
            bid.IsApplyOfferWithSubscriptionMandatory = IsSubscriptionMandatory;

            var bidDonor = await _donorService.GetBidDonorOfBidIfFound(bid.Id);

            var supervisingData = bid.BidSupervisingData
                .Where(x => x.SupervisingServiceClaimCode == SupervisingServiceClaimCodes.clm_3057 && x.SupervisorStatus == SponsorSupervisingStatus.Approved)
                .OrderByDescending(x => x.CreationDate)
                .FirstOrDefault();
            if (CheckIfWeCanPublishBidThatHasSponsor(bidDonor, supervisingData))
            {
                await ApproveBidBySupervisor(user, bid, bidDonor);
                return;
            }
            if (bid.BidTypeId == (int)BidTypes.Private)
            {
                await ApplyPrivateBidLogicWithNoSponsor(bid);
                return;
            }

            await ApplyDefaultFlowOfApproveBid(user, bid);
        }

        private OperationResult<AddBidResponse> ValidateBidDatesWhileApproving(Bid bid, ReadOnlyAppGeneralSettings generalSettings)
        {
            if (bid is not null && bid.BidAddressesTime is not null && bid.BidAddressesTime.LastDateInReceivingEnquiries.HasValue &&
                bid.BidAddressesTime.LastDateInReceivingEnquiries.Value.Date < _dateTimeZone.CurrentDate.Date)
                return OperationResult<AddBidResponse>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.LAST_DATE_IN_RECEIVING_ENQUIRIES_MUST_NOT_BE_BEFORE_TODAY_DATE);

            else if (bid.BidAddressesTime.LastDateInReceivingEnquiries > bid.BidAddressesTime.LastDateInOffersSubmission)
                return OperationResult<AddBidResponse>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.LAST_DATE_IN_OFFERS_SUBMISSION_MUST_BE_GREATER_THAN_LAST_DATE_IN_RECEIVING_ENQUIRIES);

            else if (bid.BidAddressesTime.LastDateInOffersSubmission > bid.BidAddressesTime.OffersOpeningDate)
                return OperationResult<AddBidResponse>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.OFFERS_OPENING_DATE_MUST_BE_GREATER_THAN_LAST_DATE_IN_OFFERS_SUBMISSION);

            else if (bid.BidAddressesTime.ExpectedAnchoringDate != null && bid.BidAddressesTime.ExpectedAnchoringDate != default
                && bid.BidAddressesTime.OffersOpeningDate.Value < _dateTimeZone.CurrentDate.Date)
                return OperationResult<AddBidResponse>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.EXPECTED_ANCHORING_DATE_MUST_BE_GREATER_THAN_OFFERS_OPENING_DATE_PLUS_STOPPING_PERIOD);
            else
                return OperationResult<AddBidResponse>.Success(null);
        }
        private async Task ApplyDefaultFlowOfApproveBid(ApplicationUser user, Bid bid)
        {
            await DoBusinessAfterPublishingBid(bid, _currentUserService.CurrentUser);

            await _pointEventService.AddPointEventUsageHistoryAsync(new AddPointEventUsageHistoryModel
            {
                PointType = PointTypes.PublishNonDraftBid,
                ActionId = bid.Id,
                EntityId = bid.AssociationId.HasValue ? bid.AssociationId.Value : bid.DonorId.Value,
                EntityUserType = bid.AssociationId.HasValue ? UserType.Association : UserType.Donor,
            });
            // await LogBidCreationEvent(bid);

            if (bid.TenderBrochurePoliciesType == TenderBrochurePoliciesType.UsingRFP)
                await SaveRFPAsPdf(bid);
            await _bidRepository.ExexuteAsTransaction(async () =>
            {
                await LogBidCreationEvent(bid);
                await _helperService.AddReviewedSystemRequestLog(new AddReviewedSystemRequestLogRequest()
                {
                    EntityId = bid.Id,
                    RejectionReason = null,
                    SystemRequestStatus = SystemRequestStatuses.Accepted,
                    SystemRequestType = SystemRequestTypes.BidReviewing,

                }, user);
                await _bidRepository.Update(bid);
            });
            // handle for freelancer
            await InviteProvidersWithSameCommercialSectors(bid.Id, true);
        }

        private async Task ApplyPrivateBidLogicWithNoSponsor(Bid bid)
        {
            var entityName = bid.EntityType == UserType.Association ? bid.Association?.Association_Name : bid.Donor?.DonorName;

            var currentUser = _currentUserService.CurrentUser;
            var bidInvitation = await _bidInvitationsRepository
                 .Find(a => a.BidId == bid.Id && a.InvitationStatus == InvitationStatus.New)
                 .Include(a => a.Company)
                     .ThenInclude(a => a.Provider)
                     .Include(x => x.ManualCompany)
                 .ToListAsync();
            if (bidInvitation.Any())
            {
                var _commonEmailAndNotificationService = (ICommonEmailAndNotificationService)_serviceProvider.GetService(typeof(ICommonEmailAndNotificationService));
                await _commonEmailAndNotificationService.SendInvitationsAfterApproveBid(bidInvitation.ToList(), bid, currentUser, entityName);
            }

            //=================update on list to sent ===========================================
            bidInvitation.ToList().ForEach(a =>
            {
                a.CreationDate = _dateTimeZone.CurrentDate;
                a.InvitationStatus = InvitationStatus.Sent;
                a.ModificationDate = _dateTimeZone.CurrentDate;
                a.ModifiedBy = currentUser.Id;
                a.Company = null;
            });
            await _bidInvitationsRepository.UpdateRange(bidInvitation.ToList());


            bid.CreationDate = _dateTimeZone.CurrentDate;
            bid.CreatedBy = currentUser.Id;
            // await _bidRepository.Update(bid);

            //TODO
            // if(bid.EntityType == UserType.Association)




            await DoBusinessAfterPublishingBid(bid, _currentUserService.CurrentUser);

            if (bid.TenderBrochurePoliciesType == TenderBrochurePoliciesType.UsingRFP)
                await SaveRFPAsPdf(bid);
            await _bidRepository.ExexuteAsTransaction(async () =>
            {
                await _pointEventService.AddPointEventUsageHistoryAsync(new AddPointEventUsageHistoryModel
                {
                    PointType = PointTypes.PublishNonDraftBid,
                    ActionId = bid.Id,
                    EntityId = bid.AssociationId.HasValue ? bid.AssociationId.Value : bid.DonorId.Value,
                    EntityUserType = bid.AssociationId.HasValue ? UserType.Association : UserType.Donor,
                });

                await LogBidCreationEvent(bid);
                
                await AddSystemReviewToBidByCurrentUser(bid.Id, SystemRequestStatuses.Accepted); 

                await _bidRepository.Update(bid);
            });

        }

        private static bool CheckIfWeCanPublishBidThatHasSponsor(BidDonor bidDonor, BidSupervisingData supervisingData)
        {
            return bidDonor is not null && bidDonor.DonorResponse == DonorResponse.Accept && supervisingData is not null;
        }

        private async Task SaveRFPAsPdf(Bid bid)
        {

            if (!string.IsNullOrEmpty(bid.Tender_Brochure_Policies_Url))
                await _imageService.DeleteFile(bid.Tender_Brochure_Policies_Url);
            var bidWithHtml = await _bIdWithHtmlRepository.FindOneAsync(x => x.Id == bid.Id);
            if (bidWithHtml is not null)
            {

                bidWithHtml.RFPHtmlContent = bidWithHtml.RFPHtmlContent.Replace("<span id=\"creationDateRFP\"></span>", bid.CreationDate.ToArabicFormat());
                var response = await _imageService.SaveHtmlAsFile(bidWithHtml.RFPHtmlContent, fileSettings.Bid_Attachments_FilePath, "", bid.BidName, fileSettings.MaxSizeInMega);

                bid.Tender_Brochure_Policies_FileName = response.FileName;

                bid.Tender_Brochure_Policies_Url = string.IsNullOrEmpty(response.FilePath) ?
                                            bid.Tender_Brochure_Policies_Url :
                                            await _encryptionService.DecryptAsync(response.FilePath);

                await _bIdWithHtmlRepository.Update(bidWithHtml);
            }
        }

        private async Task<List<BidAttachment>> SaveBidAttachments(AddBidAttachmentRequest model, Bid bid)
        {
            //Delete Attachments
            var existingAttachments_ContactList = await _bidAttachmentRepository.Find(x => x.BidId == model.BidId).ToListAsync();
            await _bidAttachmentRepository.DeleteRangeAsync(existingAttachments_ContactList);

            //Add Attachments
            var bidAttachmentsToSave = new List<BidAttachment>();
            if (model.LstAttachments != null && model.LstAttachments.Count > 0)
            {

                foreach (var attachment in model.LstAttachments)
                {
                    bidAttachmentsToSave.Add(new BidAttachment
                    {
                        BidId = bid.Id,
                        AttachmentName = attachment.AttachmentName,
                        AttachedFileURL = _encryptionService.Decrypt(attachment.AttachedFileURL),
                        IsDeleted = false
                    });
                }

                await _bidAttachmentRepository.AddRange(bidAttachmentsToSave);
            }

            return bidAttachmentsToSave;
        }

        private async Task<(bool IsSuceeded, string ErrorMessage, string LogRef, long AllCount, long AllNotFreeSubscriptionCount)> SendEmailToCompaniesInBidIndustry(Bid bid, string entityName, bool isAutomatically)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var helperService = scope.ServiceProvider.GetRequiredService<IHelperService>();
            var convertViewService = scope.ServiceProvider.GetRequiredService<IConvertViewService>();
            var sendinblueService = scope.ServiceProvider.GetRequiredService<ISendinblueService>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var emailSettingService = scope.ServiceProvider.GetRequiredService<IEmailSettingService>();
            var sMSService = scope.ServiceProvider.GetRequiredService<ISMSService>();
            var bidsOfProviderRepository = scope.ServiceProvider.GetRequiredService<ITenderSubmitQuotationRepositoryAsync>();
            var freelancerRepo = scope.ServiceProvider.GetRequiredService<ICrossCuttingRepository<Freelancer, long>>();
            var subscriptionPaymentRepository = scope.ServiceProvider.GetRequiredService<ICrossCuttingRepository<SubscriptionPayment, long>>();
            var appGeneralSettingsRepository = scope.ServiceProvider.GetRequiredService<ICrossCuttingRepository<AppGeneralSetting, long>>();


            var userType = UserType.Provider;
            var eventt = SystemEventsTypes.NewBidInCompanyIndustryEmail;
           

            string subject = $"تم طرح منافسة جديدة في قطاع عملكم {bid.BidName}";

            var receivers = new List<GetRecieverEmailForEntitiesInSystemDto>();
            var registeredEntitiesWithNonFreeSubscriptionsPlanIds = new List<long>();

            if ((BidTypes)bid.BidTypeId == BidTypes.Instant || (BidTypes)bid.BidTypeId == BidTypes.Public)
            {
                receivers = await bidsOfProviderRepository.GetProvidersEmailsOfCompaniesSubscribedToBidIndustries(bid);
                registeredEntitiesWithNonFreeSubscriptionsPlanIds.AddRange(receivers.Where(x => x.CompanyId.HasValue).Select(x => x.CompanyId.Value));
            }
            else if ((BidTypes)bid.BidTypeId == BidTypes.Freelancing)
            {
                userType = UserType.Freelancer;
                eventt = SystemEventsTypes.NewBidInFreelancerIndustryEmail;
                receivers = await GetFreelancersWithSameWorkingSectors(freelancerRepo, bid);
                registeredEntitiesWithNonFreeSubscriptionsPlanIds.AddRange(receivers.Select(x => x.Id));
            }
            else
                throw new ArgumentException($"This Enum Value: {((BidTypes)bid.BidTypeId).ToString()} Not Handled Here {nameof(BidServiceCore.InviteProvidersInBackground)}");

            var registeredEntitiesWithNonFreeSubscriptionsPlan = await subscriptionPaymentRepository.Find(x => !x.IsExpired && x.SubscriptionStatus != SubscriptionStatus.Expired
            && x.UserTypeId == (userType == UserType.Provider ? UserType.Company : userType)
            && registeredEntitiesWithNonFreeSubscriptionsPlanIds.Contains(x.UserId) 
            && ((x.SubscriptionAmount == 0 && !string.IsNullOrEmpty(x.CouponHash)) || x.SubscriptionPackagePlan.Price > 0))
                .OrderByDescending(x => x.CreationDate)
                .GroupBy(x => new { x.UserId, x.UserTypeId })
                .Select(x => new { x.Key.UserId, x.Key.UserTypeId })
                .ToListAsync();

            var currentEmailSettingId = (await emailSettingService.GetActiveEmailSetting()).Data;
            var model = new NewBidInCompanyIndustryEmail()
            {
                BaseBidEmailDto = await helperService.GetBaseDataForBidsEmails(bid)
            };

            var html = await convertViewService.RenderViewAsync(BaseBidEmailDto.BidsEmailsPath, NewBidInCompanyIndustryEmail.EmailTemplateName, model);
            if (fileSettings.ENVIROMENT_NAME.ToLower() == EnvironmentNames.production.ToString().ToLower()
                && currentEmailSettingId == (int)EmailSettingTypes.SendinBlue)
            {
                try
                {
                    var createdListId = await sendinblueService.CreateListOfContacts($"موردين قطاعات المنافسة ({bid.Ref_Number})", _sendinblueOptions.FolderId);
                    await sendinblueService.ImportContactsInList(new List<long?> { createdListId }, receivers);

                    var createCampaignModel = new CreateCampaignModel
                    {
                        HtmlContent = html,
                        AttachmentUrl = null,
                        CampaignSubject = subject,
                        ListIds = new List<long?> { createdListId },
                        ScheduledAtDate = null
                    };

                    var camaignResponse = await sendinblueService.CreateEmailCampaign(createCampaignModel);
                    if (!camaignResponse.IsSuccess)
                        return (camaignResponse.IsSuccess, camaignResponse.ErrorMessage, camaignResponse.LogRef, 0, 0);
                    await sendinblueService.SendEmailCampaignImmediately(camaignResponse.Id);
                }
                catch (Exception ex)
                {
                    await helperService.AddBccEmailTracker(new EmailRequestMultipleRecipients
                    {
                        Body = html,
                        Attachments = null,
                        Recipients = receivers.Select(x => new RecipientsUser { Email = x.Email, EntityId = x.Id, OrganizationEntityId = x.CompanyId, UserType = UserType.Company }).ToList(),
                        Subject = subject,
                        SystemEventType = (int)eventt,
                    }, ex);
                    throw;
                }
            }
            else
            {
                var emailRequest = new EmailRequestMultipleRecipients
                {
                    Body = html,
                    Attachments = null,
                    Recipients = receivers.Select(x => new RecipientsUser { Email = x.Email }).ToList(),
                    Subject = subject,
                    SystemEventType = (int)eventt,
                };
                await emailService.SendToMultipleReceiversAsync(emailRequest);
            }

            var nonFreeSubscriptionEntities = receivers.Where(a => registeredEntitiesWithNonFreeSubscriptionsPlan.Any(x => x.UserTypeId == UserType.Freelancer ? (a.Id == x.UserId && a.Type == x.UserTypeId) : (a.CompanyId.HasValue && a.CompanyId.Value == x.UserId && a.Type == UserType.Provider)));
            var countOfAllEntitiesWillBeSent = receivers.Count;
            var countOfNonFreeSubscriptionEntitiesWillBeSent = nonFreeSubscriptionEntities.Count();

            //send sms to provider
            string otpMessage = $"تتشرف منصة تنافُس بدعوتكم للمشاركة في منافسة {bid.BidName}، يتم استلام العروض فقط عبر منصة تنافُس. رابط المنافسة: {fileSettings.ONLINE_URL}view-bid-details/{bid.Id}";

            var recieversMobileNumbers = receivers.Select(x => x.Mobile).ToList();
            var isFeaturesEnabled = await appGeneralSettingsRepository
                                           .Find(x => true)
                                           .Select(x => x.IsSubscriptionFeaturesEnabled)
                                           .FirstOrDefaultAsync();
            if (isAutomatically&& isFeaturesEnabled)
                recieversMobileNumbers = nonFreeSubscriptionEntities.Select(x => x.Mobile).ToList();


            if (fileSettings.ENVIROMENT_NAME.ToLower() == EnvironmentNames.production.ToString().ToLower())
                recieversMobileNumbers.Add(fileSettings.SendSMSTo);

            await SendSMSForProvidersWithSameCommercialSectors(recieversMobileNumbers, otpMessage, SystemEventsTypes.PublishBidOTP, userType, true, sMSService);

            return (true, string.Empty, string.Empty, countOfAllEntitiesWillBeSent, countOfNonFreeSubscriptionEntitiesWillBeSent);
        }
        private static async Task<List<GetRecieverEmailForEntitiesInSystemDto>> GetFreelancersWithSameWorkingSectors(ICrossCuttingRepository<Freelancer, long> freelancerRepo, Bid bid)
        {
            var bidIndustries = bid.GetBidWorkingSectors().Select(x => x.ParentId);

            var receivers = await freelancerRepo.Find(x => x.IsVerified
                         && x.RegistrationStatus != RegistrationStatus.NotReviewed
                         && x.RegistrationStatus != RegistrationStatus.Rejected)
                 .Where(x => x.FreelancerWorkingSectors.Any(a => bidIndustries.Contains(a.FreelanceWorkingSector.ParentId)))
                 .Select(x => new GetRecieverEmailForEntitiesInSystemDto
                 {
                     CreationDate = x.CreationDate,
                     Email = x.Email,
                     Id = x.Id,
                     Mobile = x.MobileNumber,
                     Name = x.Name,
                     Type = UserType.Freelancer,
                 })
                 .ToListAsync();
            return receivers;
        }


        private async Task<OperationResult<bool>> SendSMSForProvidersWithSameCommercialSectors(List<string> recipients, string message, SystemEventsTypes systemEventsType, UserType userType, bool isCampaign, ISMSService sMSService)
        {
            var sendingSMSResponse = await sMSService.SendBulkAsync(new SendingSMSRequest
            {
                SMSMessage = message,
                Recipients = recipients,
                SystemEventsType = (int)systemEventsType,
                UserType = userType,
                IsCampaign = isCampaign,
            });

            return sendingSMSResponse.Data.ErrorsList.Any() ?
                OperationResult<bool>.Fail(HttpErrorCode.InvalidInput, string.Join("\n", sendingSMSResponse.Data.ErrorsList.Select(a => $"{a.Code?.Value ?? string.Empty} -- {a.ErrorMessage}")))
                : OperationResult<bool>.Success(true);
        }

        private async Task SendNotificationsOfBidAdded(ApplicationUser usr, Bid bid, string entityName)
        {
            using var scope = _serviceScopeFactory.CreateScope();

            var helperService = scope.ServiceProvider.GetRequiredService<IHelperService>();

            var bidIndustries = bid.GetBidWorkingSectors().Select(x => x.ParentId).ToList();
            var companyIndustryRepo = scope.ServiceProvider.GetRequiredService<ICrossCuttingRepository<Company_Industry, long>>();
            var companyRepo = scope.ServiceProvider.GetRequiredService<ICrossCuttingRepository<Company, long>>();
            var notificationUserClaim = scope.ServiceProvider.GetRequiredService<INotificationUserClaim>();
            var freelancerRepo = scope.ServiceProvider.GetRequiredService<ICrossCuttingRepository<Freelancer, long>>();

            List<long> entitiesIds = new List<long>();
            var orgType = OrganizationType.Comapny;
            var buyTermsBookClaimCode = ProviderClaimCodes.clm_3039.ToString();

            if ((BidTypes)bid.BidTypeId == BidTypes.Instant || (BidTypes)bid.BidTypeId == BidTypes.Public)
            {
                var companiesWithSameIndustries = await companyIndustryRepo.Find(x => bidIndustries.Contains(x.CommercialSectorsTree.ParentId.Value))
                        .Select(x => x.Company)
                        .Distinct()
                        .ToListAsync();

                if (bid.IsBidAssignedForAssociationsOnly)
                {
                    companiesWithSameIndustries = await companyRepo.Find(a => bid.EntityType == UserType.Association ?
                                                                                a.AssignedAssociationId == bid.EntityId
                                                                              : a.AssignedDonorId == bid.EntityId)
                        .Include(a => a.Provider)
                        .ToListAsync();
                }
                entitiesIds = companiesWithSameIndustries.Select(a => a.Id).ToList();
            }
            else if ((BidTypes)bid.BidTypeId == BidTypes.Freelancing)
            {
                entitiesIds = await freelancerRepo.Find(x => x.IsVerified
                            && x.RegistrationStatus != RegistrationStatus.NotReviewed
                            && x.RegistrationStatus != RegistrationStatus.Rejected)
                    .Where(x => x.FreelancerWorkingSectors.Any(a => bidIndustries.Contains(a.FreelanceWorkingSector.ParentId)))
                    .Select(x => x.Id)
                    .ToListAsync();

                orgType = OrganizationType.Freelancer;
                buyTermsBookClaimCode = FreelancerClaimCodes.clm_8001.ToString();
            }
            else
                throw new ArgumentException($"This Enum Value: {((BidTypes)bid.BidTypeId).ToString()} Not Handled Here {nameof(BidServiceCore.SendNotificationsOfBidAdded)}");


            var usersToReceiveNotify = await notificationUserClaim.GetUsersClaimOfMultipleIds(new string[] { buyTermsBookClaimCode }, entitiesIds, orgType);

            var _notificationService = (INotificationService)scope.ServiceProvider.GetService(typeof(INotificationService));
            var notificationObj = await _notificationService.GetNotificationObjModelToSendAsync(new NotificationObjModel
            {
                EntityId = bid.Id,
                Message = $"تم طرح منافسه جديده ضمن قطاعكم",
                ActualRecieverIds = usersToReceiveNotify.ActualReceivers,
                SenderId = usr.Id,
                NotificationType = NotificationType.AddBidCompany,
                ServiceType = ServiceType.Bids,
                SystemEventType = (int)SystemEventsTypes.CreateBidNotification
            });

            notificationObj.SenderName = entityName;
            notificationObj.BidName = bid.BidName;
            notificationObj.BidId = bid.Id;

            await _notificationService.SendRealTimeNotificationToUsersAsync(notificationObj, usersToReceiveNotify.RealtimeReceivers.Select(a => a.ActualRecieverId).ToList(), (int)SystemEventsTypes.CreateBidNotification);
        }

        public async Task<OperationResult<List<UploadFileResponse>>> UploadBidAttachments(IFormCollection formCollection)
        {
            var filePathLst = await _uploadingFiles.UploadAsync(formCollection, fileSettings.Bid_Attachments_FilePath, "BidAtt", fileSettings.SpecialFilesMaxSizeInMega);

            if (filePathLst.IsSucceeded)
                return OperationResult<List<UploadFileResponse>>.Success(filePathLst.Data);
            else
                return OperationResult<List<UploadFileResponse>>.Fail(filePathLst.HttpErrorCode, filePathLst.Code, filePathLst.ErrorMessage);
        }

        public async Task<OperationResult<List<UploadFileResponse>>> UploadBidAttachmentsNewsFile(IFormCollection formCollection)
        {
            var filePathLst = await _uploadingFiles.UploadAsync(formCollection, fileSettings.BidAttachmentsNewsFilePath, "Bidnews", fileSettings.MaxSizeInMega);

            if (filePathLst.IsSucceeded)
                return OperationResult<List<UploadFileResponse>>.Success(filePathLst.Data);
            else
                return OperationResult<List<UploadFileResponse>>.Fail(filePathLst.HttpErrorCode, filePathLst.Code, filePathLst.ErrorMessage);
        }

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

        public async Task<PagedResponse<IReadOnlyList<ReadOnlyFilterBidModel>>> GetBidsList(FilterBidsSearchModel request)
        {
            try
            {

                var user = _currentUserService.CurrentUser;
                if (user == null)
                {
                    return new PagedResponse<IReadOnlyList<ReadOnlyFilterBidModel>>(HttpErrorCode.NotAuthenticated);
                }

                if (request.PublishDateFrom > request.PublishDateTo)
                    return new PagedResponse<IReadOnlyList<ReadOnlyFilterBidModel>>(HttpErrorCode.InvalidInput, BidErrorCodes.PUBLISH_DATE_FROM_MUST_BE_EQUAL_TO_OR_BEFORE_PUBLISH_DATE_TO);
                if (request.OfferSubmissionDateFrom > request.OfferSubmissionDateTo)
                    return new PagedResponse<IReadOnlyList<ReadOnlyFilterBidModel>>(HttpErrorCode.InvalidInput, BidErrorCodes.OFFER_SUBMISSION_DATE_FROM_MUST_BE_EQUAL_TO_OR_BEFORE_OFFER_SUBMISSION_DATE_TO);

                var listOfAdministrationUserTypes = new List<UserType> { UserType.SuperAdmin, UserType.Admin, UserType.SupportManager, UserType.SupportMember };


                IQueryable<Bid> bids = listOfAdministrationUserTypes.Contains(user.UserType) ?
                   _bidRepository.Find(x => true, false, nameof(Bid.BidAddressesTime), nameof(Bid.Association), nameof(Bid.BidMainClassificationMapping), nameof(Bid.BidStatus), nameof(Bid.BidType), nameof(Bid.BidRegions))
                 : _bidRepository.Find(x => x.BidTypeId == (int)BidTypes.Public, false, nameof(Bid.BidAddressesTime), nameof(Bid.Association), nameof(Bid.BidMainClassificationMapping), nameof(Bid.BidStatus), nameof(Bid.BidType), nameof(Bid.BidRegions));



                IEnumerable<Bid> combinedBidList = new List<Bid>();
                Company company = null;

                if (user.UserType == UserType.Provider)
                {
                    if (user.OrgnizationType == (int)OrganizationType.Comapny)
                    {
                        company = await _companyRepository.FindOneAsync(x => x.Id == user.CurrentOrgnizationId, false);

                        if (company == null)
                        {
                            var provider = await _providerRepository.Find(p => p.UserId == user.Id).Select(x => x.Id).FirstOrDefaultAsync();
                            company = await _companyRepository.FindOneAsync(x => x.ProviderId == provider, false);

                        }

                        if (company != null)
                        {
                            var bidInvitations = await _bidInvitationsRepository.Find(x => x.CompanyId == company.Id && x.InvitationStatus == InvitationStatus.Sent, false)
                                .Select(x => x.BidId).ToListAsync();
                            //It will get all closedBidsProvider where Id is in bidInvitations list
                            var closedBidsProvider = _bidRepository.Find(x => x.BidTypeId != (int)BidTypes.Public && x.BidStatusId != (int)TenderStatus.Draft, false,
                                nameof(Bid.BidAddressesTime),
                                nameof(Bid.Association),
                                nameof(Bid.BidStatus),
                                nameof(Bid.BidMainClassificationMapping),
                                nameof(Bid.BidType),
                                nameof(Bid.BidRegions)).Where(p => bidInvitations.Contains(p.Id));

                            bids = bids.Union(closedBidsProvider);
                            //execlude draft bids for provider
                            bids = bids.Where(x => x.BidStatusId != (int)TenderStatus.Draft);

                        }
                    }
                }
                else if (user.UserType == UserType.Company)
                {
                    var userCompany = await _companyService.GetUserCompany(user.Email);
                    if (userCompany != null)
                    {
                        company = userCompany;
                        var bidInvitations = await _bidInvitationsRepository.Find(x => x.CompanyId == userCompany.Id, false)
                            .Select(x => x.BidId).ToListAsync();

                        //It will get all closedBidsProvider where Id is in bidInvitations list
                        var closedBidsProvider = _bidRepository.Find(x => x.BidTypeId != (int)BidTypes.Public && x.BidStatusId == (int)TenderStatus.Open, false,
                                nameof(Bid.BidAddressesTime),
                                nameof(Bid.Association),
                                nameof(Bid.BidStatus),
                                nameof(Bid.BidMainClassificationMapping),
                                nameof(Bid.BidType),
                                nameof(Bid.BidRegions)).Where(p => bidInvitations.Contains(p.Id));

                        bids = bids.Union(closedBidsProvider);
                        //execlude draft bids for provider
                        bids = bids.Where(x => x.BidStatusId != (int)TenderStatus.Draft);


                    }
                }

                long currentAssociationId = 0;
                if (user.UserType == UserType.Association)
                {
                    var association = await _associationService.GetUserAssociation(user.Email);
                    if (association != null)
                    {
                        currentAssociationId = association.Id;
                        var bidInvitations = _bidInvitationsRepository.Find(x => x.Bid.AssociationId == association.Id, false
                        /*nameof(BidInvitations.Bid)*/).Select(x => x.BidId);

                        var closedBidsAssociation = _bidRepository.Find(x => (x.AssociationId == association.Id || x.SupervisingAssociationId == association.Id) && x.BidTypeId != (int)BidTypes.Public && (x.BidStatusId == (int)TenderStatus.Open || x.BidStatusId == (int)TenderStatus.Pending), false,
                            nameof(Bid.BidAddressesTime),
                            nameof(Bid.Association),
                            nameof(Bid.BidStatus),
                            nameof(Bid.BidMainClassificationMapping),
                            nameof(Bid.BidType),
                            nameof(Bid.BidRegions)).Where(x => bidInvitations.Contains(x.Id));


                        bids = bids.Where(x => !(x.BidStatusId == (int)TenderStatus.Pending && currentAssociationId != x.AssociationId))
                            .Union(closedBidsAssociation);

                        var OthersDraftedBids = bids
                            .Where(b => b.BidStatusId == (int)TenderStatus.Draft && b.AssociationId != currentAssociationId);


                        bids = bids.Where(b => !OthersDraftedBids.Any(x => x.Id == b.Id));

                    }
                }


                // execlude pending bids if user is super admin
                if (listOfAdministrationUserTypes.Contains(user.UserType))
                    bids = bids.Where(a => a.BidStatusId != (int)TenderStatus.Pending);

                bids = await ApplyFiltrationForBids(request, bids);

                var BidsCount = await bids.CountAsync();
                try
                {
                    if (bids == null || BidsCount == 0)
                    {
                        return new PagedResponse<IReadOnlyList<ReadOnlyFilterBidModel>>(null, request.pageNumber, request.pageSize);
                    }
                }
                catch
                {
                    return new PagedResponse<IReadOnlyList<ReadOnlyFilterBidModel>>(null, request.pageNumber, request.pageSize);
                }
                bids = bids.OrderByDescending(a => a.CreationDate);

                //int totalRecords = combinedBidList.Count();
                int totalRecords = BidsCount;
                bids = bids.OrderByDescending(a => a.CreationDate).Skip((request.pageNumber - 1) * request.pageSize).Take(request.pageSize);


                var bidsModels = (_mapper.Map<List<ReadOnlyFilterBidModel>>(await bids.ToListAsync()));

                foreach (var bid in bids)
                {
                    var bidMod = bidsModels.FirstOrDefault(bidMod => bidMod.Id == bid.Id);

                    if (bidMod != null)
                        bidMod.Regions = BidRegion.getAllRegionsAsListOfIds(bid.BidRegions);
                }





                foreach (var x in bidsModels)
                {
                    x.Association.ImageResponse = await _imageService.GetFileResponseEncrypted(x.Association.Image, x.Association.ImageFileName);
                    x.IsCurrentAssociation = x.AssociationId == currentAssociationId ? true : false;

                }


                if (user != null)
                {
                    var userFavBids = (await _userFavBidList.Find(x => x.UserId == user.Id).ToListAsync()).ToDictionary(x => x.BidId);

                    var bidsIds = bidsModels.Select(x => x.Id);
                    var awardingSelections = await _awardingSelectRepository.Find(x => bidsIds.Contains(x.BidId)).ToListAsync();
                    var tendersSubmitsQutations = await _tenderSubmitQuotationRepository.Find(x => bidsIds.Contains(x.BidId)).ToListAsync();
                    var providersBids = await _providerBidRepository.Find(x => bidsIds.Contains(x.BidId) && x.IsPaymentConfirmed == true).ToListAsync();


                    foreach (var itm in bidsModels)
                    {

                        if (userFavBids.ContainsKey(itm.Id))
                            itm.IsUserFavorite = true;
                        var awardingSelection = awardingSelections.Where(x => x.BidId == itm.Id);
                        //added Flag(IsBidAwarded)-->to know if bid is awarded or not 
                        itm.IsBidAwarded = (awardingSelection.Count()) > 0 ? true : false;

                        if (user.UserType == UserType.Provider || user.UserType == UserType.Company)
                        {
                            if (company != null)
                            {
                                itm.IsBuyRFI = providersBids.Where(x => x.BidId == itm.Id && x.CompanyId == company.Id).Count() > 0 ? true : false;

                                var tSub = (tendersSubmitsQutations.Where(x => x.BidId == itm.Id && x.CompanyId == company.Id && x.ProposalStatus == ProposalStatus.Delivered).Select(x => x.Id)
                                .FirstOrDefault());


                                itm.IsApplyForBid = tSub > 0 ? true : false;




                                itm.QuotationId = tSub;
                                if (!itm.IsApplyForBid)
                                {
                                    var tenderQuotationOrdered = tendersSubmitsQutations.Where(x => x.BidId == itm.Id && x.CompanyId == company.Id)
                                        .OrderByDescending(a => a.CreationDate).Select(x => x.Id).FirstOrDefault();
                                    if (tenderQuotationOrdered > 0)
                                        itm.QuotationId = tenderQuotationOrdered;
                                }
                            }
                        }

                        var tenderCount = tendersSubmitsQutations.Where(x => x.BidId == itm.Id && x.ProposalStatus == ProposalStatus.Delivered);
                        itm.TenderQuotationsCount = tenderCount.Count();
                    }
                }

                return new PagedResponse<IReadOnlyList<ReadOnlyFilterBidModel>>(bidsModels, request.pageNumber, request.pageSize, totalRecords);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = request,
                    ErrorMessage = "Failed to search bids!",
                    ControllerAndAction = "BidController/GetAll"
                });
                return new PagedResponse<IReadOnlyList<ReadOnlyFilterBidModel>>(
                       HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }

        public async Task<OperationResult<ReadOnlyBidModel>> GetBidDetails(long id)
        {
            try
            {
                var bid = await _bidRepository.FindOneAsync(x => x.Id == id, false, nameof(Bid.BidAddressesTime),
                    nameof(Bid.Association),
                    nameof(Bid.BidStatus),
                    nameof(Bid.BidNews),
                    nameof(Bid.BidAttachment),
                    nameof(Bid.InvitationRequiredDocuments),
                    nameof(Bid.BidRegions)
                    );

                if (bid == null)
                {
                    return OperationResult<ReadOnlyBidModel>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.NOT_FOUND);
                }
                var model = _mapper.Map<ReadOnlyBidModel>(bid);

                model.Regions = BidRegion.getAllRegionsAsListOfIds(bid.BidRegions);

                if (bid.BidTypeId != (int)BidTypes.Habilitation && model.BidAddressesTime != null)
                    model.BidAddressesTime.InvitationDocumentsApplyingEndDate = bid.InvitationDocumentsApplyingEndDate?.ToString("yyyy-MM-dd");




                return OperationResult<ReadOnlyBidModel>.Success(model);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = $"Bid ID = {id}",
                    ErrorMessage = "Failed to Get Bid Details By Id!",
                    ControllerAndAction = "BidController/GetDetails"
                });
                return OperationResult<ReadOnlyBidModel>.Fail(
                       HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }


        public async Task<OperationResult<ReadOnlyBidAddressesTimeModel>> GetBidAddressesTime(long bidId)
        {
            try
            {
                var bidAddressesTime = await _bidAddressesTimeRepository.FindOneAsync(x => x.BidId == bidId, false, nameof(BidAddressesTime.Bid));
                var bid = await _bidRepository.FindOneAsync(x => x.Id == bidId);


                if (bidAddressesTime == null && bid != null)
                {
                    return OperationResult<ReadOnlyBidAddressesTimeModel>.Success(new ReadOnlyBidAddressesTimeModel()
                    {
                        BidVisibility = (BidTypes)bid.BidTypeId,
                        BidId = bid.Id,
                        InvitationDocumentsApplyingEndDate = bid.InvitationDocumentsApplyingEndDate?.ToString("yyyy-MM-dd")
                    });
                }

                if (bidAddressesTime == null)
                {
                    return OperationResult<ReadOnlyBidAddressesTimeModel>.Success(null);
                }
                var model = _mapper.Map<ReadOnlyBidAddressesTimeModel>(bidAddressesTime);
                model.ExpectedAnchoringDate = bidAddressesTime.ExpectedAnchoringDate != null ? bidAddressesTime.ExpectedAnchoringDate.Value.ToString("yyyy-MM-dd") : "";
                //model.OffersInvestigationDate = bidAddressesTime.OffersInvestigationDate.Value.Date.ToString("yyyy-MM-dd");
                //   model.OffersInvestigationDate = bidAddressesTime.OffersInvestigationDate.Date.ToShortDateString();
                model.OffersOpeningDate = ((DateTime)bidAddressesTime.OffersOpeningDate).ToString("yyyy-MM-dd");
                //model.WorkStartDate = bidAddressesTime.WorkStartDate != null ? bidAddressesTime.WorkStartDate.Value.ToString("yyyy-MM-dd") : "";
                model.LastDateInOffersSubmission = ((DateTime)bidAddressesTime.LastDateInOffersSubmission).ToString("yyyy-MM-dd");
                // model.ConfirmationLetterDueDate = bidAddressesTime.ConfirmationLetterDueDate != null ? bidAddressesTime.ConfirmationLetterDueDate.Value.ToString("yyyy-MM-dd") : "";
                model.LastDateInReceivingEnquiries = ((DateTime)bidAddressesTime.LastDateInReceivingEnquiries).ToString("yyyy-MM-dd");
                model.EnquiriesStartDate = ((DateTime)bidAddressesTime.EnquiriesStartDate).ToString("yyyy-MM-dd");
                model.InvitationDocumentsApplyingEndDate = bidAddressesTime.Bid.InvitationDocumentsApplyingEndDate?.ToString("yyyy-MM-dd");
                model.BidVisibility = (BidTypes)bidAddressesTime.Bid.BidTypeId;
                return OperationResult<ReadOnlyBidAddressesTimeModel>.Success(model);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = $"Bid ID = {bidId}",
                    ErrorMessage = "Failed to Get Bid Addresses Time!",
                    ControllerAndAction = "BidController/GetBidAddressesTime/{id}"
                });
                return OperationResult<ReadOnlyBidAddressesTimeModel>.Fail(
                       HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }

        public async Task<OperationResult<ReadOnlyBidAttachmentRequest>> GetBidAttachment(long bidId)
        {
            try
            {
                var user = _currentUserService.CurrentUser;
                if (user is null)
                    return OperationResult<ReadOnlyBidAttachmentRequest>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);

                var isUserHasAccess = await checkIfParticipantCanAccessBidData(bidId, user);
                if (!isUserHasAccess.IsSucceeded)
                    return OperationResult<ReadOnlyBidAttachmentRequest>.Fail(isUserHasAccess.HttpErrorCode, isUserHasAccess.Code);
                var bidAttachment = await _bidAttachmentRepository.FindAsync(x => x.BidId == bidId, false);
                var bid = await _bidRepository.FindOneAsync(x => x.Id == bidId, false, nameof(Bid.BidAddressesTime),
                  nameof(Bid.Association),
                  nameof(Bid.BidStatus),
                  nameof(Bid.BidNews), nameof(Bid.BidAttachment)
                  );

                if (bid == null)
                    return OperationResult<ReadOnlyBidAttachmentRequest>.Success(new ReadOnlyBidAttachmentRequest()
                    {
                        BidVisibility = (BidTypes)bid.BidTypeId,
                        BidId = bid.Id,
                    });

                var bidData = _mapper.Map<List<ReadOnlyBidAttachmentModel>>(bidAttachment);
                ReadOnlyBidAttachmentRequest model = new ReadOnlyBidAttachmentRequest();
                model.LstAttachments = bidData;
                model.BidId = bidId;
                model.Tender_Brochure_Policies_FileName = bid.Tender_Brochure_Policies_FileName;
                model.Tender_Brochure_Policies_Url = await _imageService.GetFileResponseEncrypted(bid.Tender_Brochure_Policies_Url, bid.Tender_Brochure_Policies_FileName);
                model.BidVisibility = (BidTypes)bid.BidTypeId;
                model.TenderBrochurePoliciesType = bid.TenderBrochurePoliciesType;
                model.RFPId = bid.RFPId;
                model.BidVisibility = (BidTypes)bid.BidTypeId;

                foreach (var item in model.LstAttachments)
                {
                    item.AttachedFileURLResponse = await _imageService.GetFileResponseEncrypted(item.AttachedFileURL, item.AttachmentName);
                    item.AttachedFileURL = item.AttachedFileURLResponse.FilePath;
                }

                return OperationResult<ReadOnlyBidAttachmentRequest>.Success(model);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = $"Bid ID = {bidId}",
                    ErrorMessage = "Failed to Get Bid Attachment!",
                    ControllerAndAction = "BidController/GetBidAttachment/{id}"
                });
                return OperationResult<ReadOnlyBidAttachmentRequest>.Fail(
                       HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }

        private async Task<OperationResult<ReadOnlyBidAttachmentRequest>> GetBidAttachmentNew(Bid bid,ApplicationUser usr)
        {
          
            var bidAttachment = await _bidAttachmentRepository.FindAsync(x => x.BidId == bid.Id, false);

            if (bid == null)
                return OperationResult<ReadOnlyBidAttachmentRequest>.Success(new ReadOnlyBidAttachmentRequest()
                {
                    BidVisibility = (BidTypes)bid.BidTypeId,
                    BidId = bid.Id,
                });

            var bidData = _mapper.Map<List<ReadOnlyBidAttachmentModel>>(bidAttachment);
            ReadOnlyBidAttachmentRequest model = new ReadOnlyBidAttachmentRequest();
            model.LstAttachments = bidData;
            model.BidId = bid.Id;
            model.Tender_Brochure_Policies_FileName = bid.Tender_Brochure_Policies_FileName;
            model.Tender_Brochure_Policies_Url = await _imageService.GetFileResponseEncrypted(bid.Tender_Brochure_Policies_Url, bid.Tender_Brochure_Policies_FileName);
            model.BidVisibility = (BidTypes)bid.BidTypeId;
            model.TenderBrochurePoliciesType = bid.TenderBrochurePoliciesType;
            model.RFPId = bid.RFPId;
            model.BidVisibility = (BidTypes)bid.BidTypeId;

            foreach (var item in model.LstAttachments)
            {
                item.AttachedFileURLResponse = await _imageService.GetFileResponseEncrypted(item.AttachedFileURL, item.AttachmentName);
                item.AttachedFileURL = item.AttachedFileURLResponse.FilePath;
            }

            return OperationResult<ReadOnlyBidAttachmentRequest>.Success(model);
        }


        public async Task<OperationResult<IReadOnlyList<ReadOnlyBidNewsModel>>> GetBidNews(long bidId)
        {
            try
            {
                var bidNews = _bidNewsRepository.Find(x => x.BidId == bidId, false);
                if (bidNews == null)
                {
                    return OperationResult<IReadOnlyList<ReadOnlyBidNewsModel>>.Success(null);
                }
                //  bidNews.ToList().ForEach(x => x.Image = !string.IsNullOrEmpty(x.Image) ? fileSettings.BASE_URL + x.Image : x.Image);
                var model = _mapper.Map<IReadOnlyList<ReadOnlyBidNewsModel>>(bidNews);
                foreach (var item in model)
                {
                    var img = bidNews.Where(a => a.Id == item.Id).FirstOrDefault();
                    item.ImageResponse = await _imageService.GetFileResponseEncrypted(img.Image, img.ImageFileName);
                }

                return OperationResult<IReadOnlyList<ReadOnlyBidNewsModel>>.Success(model);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = $"Bid ID = {bidId}",
                    ErrorMessage = "Failed to Get Bid News!",
                    ControllerAndAction = "BidController/GetBidNews/{id}"
                });
                return OperationResult<IReadOnlyList<ReadOnlyBidNewsModel>>.Fail(
                       HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }

        public async Task<OperationResult<List<ReadOnlyQuantitiesTableModel>>> GetBidQuantitiesTable(long bidId)
        {
            try
            {

                var isUserHasAccess = await checkIfParticipantCanAccessBidData(bidId, _currentUserService.CurrentUser);
                if (!isUserHasAccess.IsSucceeded)
                    return OperationResult<List<ReadOnlyQuantitiesTableModel>>.Fail(isUserHasAccess.HttpErrorCode, isUserHasAccess.Code);
                var bidQuantitiesTable = await _bidQuantitiesTableRepository.FindAsync(x => x.BidId == bidId, false);

                if (bidQuantitiesTable == null)
                {
                    return OperationResult<List<ReadOnlyQuantitiesTableModel>>.Success(null);
                }
                var model = _mapper.Map<List<ReadOnlyQuantitiesTableModel>>(bidQuantitiesTable);

                return OperationResult<List<ReadOnlyQuantitiesTableModel>>.Success(model);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = $"Bid ID = {bidId}",
                    ErrorMessage = "Failed to Get Bid Quantities Table!",
                    ControllerAndAction = "BidController/GetBidQuantitiesTable/{id}"
                });
                return OperationResult<List<ReadOnlyQuantitiesTableModel>>.Fail(
                       HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }

        public async Task<OperationResult<ReadOnlyBidMainDataModel>> GetBidMainData(long id)
        {
            try
            {
                var user = _currentUserService.CurrentUser;
                if (user is null)
                    return await MapPublicMainData(id);
                var bid = await GetBidWithRelatedEntitiesByIdAsync(id);

                if (bid is null)
                    return OperationResult<ReadOnlyBidMainDataModel>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);

                // mapping
                ReadOnlyBidMainDataModel model = await MapBasicDataForBidMainData(bid);

                //=======================Payment Info===========================

                if (user != null)
                {
                    Company userCompany = new Company();
                    if (user.UserType == UserType.Provider)
                    {
                        if (user.OrgnizationType == (int)OrganizationType.Comapny)
                            userCompany = await _companyRepository.FindOneAsync(x => x.Id == user.CurrentOrgnizationId, false);
                        else
                            userCompany = await _companyService.GetUserCompany(user.Email);

                        if (userCompany != null)
                        {
                            var providerBidOfCurrentCompany = await _providerBidRepository.FindOneAsync(a => a.BidId == bid.Id &&
                            a.CompanyId == userCompany.Id
                            && a.IsPaymentConfirmed);
                            if (providerBidOfCurrentCompany is not null)
                            {
                                // ProviderBid paymentInfo = bid.ProviderBids.FirstOrDefault(a => a.CompanyId == userCompany.Id );
                                model.TransactionNumber = providerBidOfCurrentCompany.TransactionNumber;
                                model.TransactionDate = providerBidOfCurrentCompany.CreationDate.ToString("yyyy-MM-dd");
                            }
                        }
                    }
                }


                //==============================================================
                model.BidVisibilityName = EnumArabicNameExtensions.GetArabicNameFromEnum((BidTypes)bid.BidTypeId);
                model.InvitationDocumentsApplyingEndDate = bid.InvitationDocumentsApplyingEndDate?.ToString("yyyy-MM-dd HH:mm:ss.fffffff");

                if (bid.BidAddressesTime != null)
                {
                    model.LastDateInOffersSubmission = bid.BidAddressesTime.LastDateInOffersSubmission != null ? ((DateTime)bid.BidAddressesTime.LastDateInOffersSubmission).ToString("yyyy-MM-dd HH:mm:ss.fffffff") : "";
                    model.ExpectedAnchoringDate = bid.BidAddressesTime.ExpectedAnchoringDate != null ? bid.BidAddressesTime.ExpectedAnchoringDate.Value.ToString("yyyy-MM-dd HH:mm:ss.fffffff") : "";
                    model.OffersOpeningDate = bid.BidAddressesTime.OffersOpeningDate != null ? ((DateTime)bid.BidAddressesTime.OffersOpeningDate).ToString("yyyy-MM-dd HH:mm:ss.fffffff") : "";
                    model.LastDateInReceivingEnquiries = bid.BidAddressesTime.LastDateInReceivingEnquiries != null ? ((DateTime)bid.BidAddressesTime.LastDateInReceivingEnquiries).ToString("yyyy-MM-dd HH:mm:ss.fffffff") : "";
                    model.EnquiriesStartDate = bid.BidAddressesTime.EnquiriesStartDate != null ? ((DateTime)bid.BidAddressesTime.EnquiriesStartDate).ToString("yyyy-MM-dd HH:mm:ss.fffffff") : "";
                }

                model.TenderQuotationsCount = (user.UserType != UserType.Provider)
                    || (user.UserType == UserType.Provider && bid.BidTypeId == (int)BidTypes.Instant && bid.isLimitedOffers == true) ?
                    await _tenderSubmitQuotationRepository.Find(x => x.BidId == bid.Id && x.ProposalStatus == ProposalStatus.Delivered, false)
                    .CountAsync() : null;

                var providerBidAggregations = await _providerBidRepository
                                   .Find(x => x.BidId == bid.Id && x.IsPaymentConfirmed)
                                   .GroupBy(x => x.BidId)
                                   .Select(pb => new
                                   {
                                       TotalBidDocumentsCount = pb.Count(),
                                       TotalBidDocumentsPrice = pb.Sum(x => x.Price)
                                   }).FirstOrDefaultAsync();
                if (providerBidAggregations is not null && user.UserType != UserType.Provider)
                {
                    model.TotalBidDocumentsPrice = Math.Round(providerBidAggregations.TotalBidDocumentsPrice, 2);
                    model.TotalBidDocumentsCount = providerBidAggregations.TotalBidDocumentsCount;

                }
                await FillBidDonorInfo(bid, model);

                model.ISEditable = CheckIfBidIsEditable(bid, user);

                model.CancelationReason = bid.BidCancelationReason?.CancelationReason;

                await FillSupervisingInfo(bid, model);

                // Get Bid Attachment Here To Reduce Mutible Calling Endpoints
                var getBidAttachmetnsResult = await GetBidAttachmentNew(bid,user);
                model.HasAttachments = (getBidAttachmetnsResult?.Data?.LstAttachments?.Any()??false )
                    || getBidAttachmetnsResult?.Data?.Tender_Brochure_Policies_Url!=null;



                model.BidAttachments = user.UserType== UserType.Provider?null: getBidAttachmetnsResult.Data;


                //get rating data
                model.AverageRating = bid.Association is not null ? (bid.Association.AverageRating == 0 ? 5 : bid.Association.AverageRating) : (bid.Donor.AverageRating == 0 ? 5 : bid.Donor.AverageRating);
                model.TotalRatings = bid.Association is not null ? (bid.Association.TotalRatings) : bid.Donor.TotalRatings;
                return OperationResult<ReadOnlyBidMainDataModel>.Success(model);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = $"Bid ID = {id}",
                    ErrorMessage = "Failed to Get Bid Main Data!",
                    ControllerAndAction = "BidController/GetBidMainData/{id}"
                });
                return OperationResult<ReadOnlyBidMainDataModel>.Fail(
                       HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }

        private async Task<ReadOnlyBidMainDataModel> MapBasicDataForBidMainData(Bid bid)
        {
            var model = _mapper.Map<ReadOnlyBidMainDataModel>(bid);

            model.InvitedAssociationByDonor = await this.GetInvitedAssociationIfFound(bid);

            model.BidOffersSubmissionTypeName = bid.BidOffersSubmissionType?.NameAr;

            model.Regions = BidRegion.getAllRegionsAsListOfIds(bid.BidRegions);
            model.RegionsNames = bid.BidRegions.Select(b => b.Region.NameAr).ToList();
            int regionCount = await _regionRepository.Find(a => true, true, false).CountAsync();
            if (model.RegionsNames.Count == regionCount)
            {
                model.RegionsNames.Clear();
                model.RegionsNames.Add(Constants.AllRegionsArabic);
            }

            var bidWorkingSectors = bid.GetBidWorkingSectors();

            model.BidMainClassificationId = bidWorkingSectors.Select(a => new BidMainClassificationIds { Id = a.Id, ParentId = a.ParentId }).ToList();
            model.BidMainClassificationNames = bidWorkingSectors.Select(i => new BidMainClassificationNames { Name = i.NameAr, ParentName = i?.Parent?.NameAr }).ToList();
            model.IndustriesIds = bidWorkingSectors.Select(x => x.Id).ToList();
            model.BidMainClassifications = (bidWorkingSectors.Where(i => model.BidMainClassificationId.Select(x => x.Id).ToList().Contains(i.Id)))
                .Select(i => new BidMainClassificationDTO { Name = i.NameAr, Id = i.Id, ParentName = i?.Parent?.NameAr, ParentId = i?.Parent?.Id }).ToList();

            return model;
        }

        private async Task<OperationResult<ReadOnlyBidMainDataModel>> MapPublicMainData(long id)
        {
            var bid = await GetBidWithRelatedEntitiesByIdAsync(id);

            if (bid is null)
                return OperationResult<ReadOnlyBidMainDataModel>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);

            ReadOnlyBidMainDataModel model = await MapBasicDataForBidMainData(bid);
            //remove non public data in case of anonymous user.
            model.IsFinancialInsuranceRequired = null;
            model.FinancialInsuranceValue = null;
            //=======================Payment Info===========================



            //==============================================================
            model.BidVisibilityName = EnumArabicNameExtensions.GetArabicNameFromEnum((BidTypes)bid.BidTypeId);
            model.InvitationDocumentsApplyingEndDate = bid.InvitationDocumentsApplyingEndDate?.ToString("yyyy-MM-dd HH:mm:ss.fffffff");

            if (bid.BidAddressesTime != null)
            {
                model.LastDateInOffersSubmission = bid.BidAddressesTime.LastDateInOffersSubmission != null ? ((DateTime)bid.BidAddressesTime.LastDateInOffersSubmission).ToString("yyyy-MM-dd HH:mm:ss.fffffff") : "";
                model.ExpectedAnchoringDate = bid.BidAddressesTime.ExpectedAnchoringDate != null ? bid.BidAddressesTime.ExpectedAnchoringDate.Value.ToString("yyyy-MM-dd HH:mm:ss.fffffff") : "";
                model.OffersOpeningDate = ((DateTime)bid.BidAddressesTime.OffersOpeningDate).ToString("yyyy-MM-dd HH:mm:ss.fffffff");
                model.LastDateInReceivingEnquiries = ((DateTime)bid.BidAddressesTime.LastDateInReceivingEnquiries).ToString("yyyy-MM-dd HH:mm:ss.fffffff");
                model.EnquiriesStartDate = ((DateTime)bid.BidAddressesTime.EnquiriesStartDate).ToString("yyyy-MM-dd HH:mm:ss.fffffff");
            }
            //model.TenderQuotationsCount = await _tenderSubmitQuotationRepository.Find(x => x.BidId == bid.Id && x.ProposalStatus == ProposalStatus.Delivered, false)
            //    .CountAsync();
            //var providerBidAggregations = await _providerBidRepository
            //                   .Find(x => x.BidId == bid.Id && x.IsPaymentConfirmed)
            //                   .GroupBy(x => x.BidId)
            //                   .Select(pb => new
            //                   {
            //                       TotalBidDocumentsCount = pb.Count(),
            //                       TotalBidDocumentsPrice = pb.Sum(x => x.Price)
            //                   }).FirstOrDefaultAsync();
            //if (providerBidAggregations is not null)
            //{
            //    model.TotalBidDocumentsPrice = providerBidAggregations.TotalBidDocumentsPrice;
            //    model.TotalBidDocumentsCount = providerBidAggregations.TotalBidDocumentsCount;

            //}
            // await FillBidDonorInfo(bid, model);



            model.CancelationReason = bid.BidCancelationReason?.CancelationReason;

            //await FillSupervisingInfo(bid, model);

            // Get Bid Attachment Here To Reduce Mutible Calling Endpoints
            model.EntityId = 0;
            model.EntityType = 0;
            //get rating data
            model.AverageRating = bid.Association is not null ? (bid.Association.AverageRating == 0 ? 5 : bid.Association.AverageRating) : (bid.Donor.AverageRating == 0 ? 5 : bid.Donor.AverageRating);
            model.TotalRatings = bid.Association is not null ? (bid.Association.TotalRatings) : (bid.Donor.TotalRatings);
            return OperationResult<ReadOnlyBidMainDataModel>.Success(model);
        }

        private static bool CheckIfBidIsEditable(Bid model, ApplicationUser user)
        {
            bool ISEditable = false;
            if (user is not null)
            {
                ISEditable = (((user.UserType == UserType.SuperAdmin || user.UserType == UserType.Admin) && model.BidStatusId != (int)TenderStatus.Draft && model.BidStatusId != (int)TenderStatus.Open && model.BidStatusId != (int)TenderStatus.Reviewing)
                    || (((user.UserType == UserType.Association || user.UserType == UserType.Donor) && model.BidStatusId != (int)TenderStatus.Draft && model.BidStatusId != (int)TenderStatus.Rejected))) ? false : true;

                if (user.UserType != UserType.SuperAdmin && user.UserType != UserType.Admin &&
                    user.UserType != UserType.Association && user.UserType != UserType.Donor)
                    ISEditable = false;
            }
            return ISEditable;
            //model.ISEditable =
            //    (user.UserType != UserType.SuperAdmin || model.BidStatusId == (int)TenderStatus.Draft || model.BidStatusId == (int)TenderStatus.Open)
            //    && (user.UserType != UserType.Association || model.BidStatusId == (int)TenderStatus.Draft || model.BidStatusId == (int)TenderStatus.Rejected);
        }

        private async Task FillSupervisingInfo(Bid bid, ReadOnlyBidMainDataModel model)
        {
            if (bid.EntityType == UserType.Donor)
            {
                if (bid.SupervisingAssociationId.HasValue)
                {
                    if (bid.SupervisingAssociationId.Value <= 0 || (bid.IsSupervisingAssociationInvited && !bid.SupervisingAssociationId.HasValue)) // To include data for invited Association but not yet registered.
                    {
                        model.SupervisorName = await _invitedAssociationsByDonorRepository.Find(a => a.BidId == bid.Id).Select(a => a.AssociationName).FirstOrDefaultAsync();
                    }
                    else
                        model.SupervisorName = await _associationRepository.Find(a => a.Id == bid.SupervisingAssociationId.Value).Select(a => a.Association_Name).FirstOrDefaultAsync();
                }
            }
            else if (bid.EntityType == UserType.Association)
                model.SupervisorName = model.DonorRequest?.NewDonorName;
        }

        private async Task FillBidDonorInfo(Bid bid, ReadOnlyBidMainDataModel model)
        {
            if (bid.IsFunded)
            {
                BidDonor bidDonor = await _BidDonorRepository
                    .Find(a => a.BidId == bid.Id, false, nameof(BidDonor.Donor))
                    .OrderByDescending(a => a.CreationDate)
                    .FirstOrDefaultAsync();

                model.DonorRequest = bidDonor is not null
                ? new BidDonorRequest
                {
                    BidDonorId = bidDonor.Id,
                    DonorId = bidDonor.DonorId ?? 0,
                    NewDonorName = bidDonor.DonorId == null ? bidDonor.NewDonorName : bidDonor.Donor.DonorName,
                    Email = bidDonor.DonorId == null ? bidDonor.Email : bidDonor.Donor.DonorEmail,
                    PhoneNumber = bidDonor.DonorId == null ? bidDonor.PhoneNumber : bidDonor.Donor.DonorNumber
                }
                : null;
            }
        }

        public async Task<PagedResponse<IReadOnlyList<ReadOnlyFilterBidModel>>> GetBidsCreatedByUser(GetBidsCreatedByUserModel request)
        {
            try
            {
                var user = _currentUserService.CurrentUser;
                if (request.PublishDateFrom > request.PublishDateTo)
                    return new PagedResponse<IReadOnlyList<ReadOnlyFilterBidModel>>(HttpErrorCode.InvalidInput, BidErrorCodes.PUBLISH_DATE_FROM_MUST_BE_EQUAL_TO_OR_BEFORE_PUBLISH_DATE_TO);
                if (request.OfferSubmissionDateFrom > request.OfferSubmissionDateTo)
                    return new PagedResponse<IReadOnlyList<ReadOnlyFilterBidModel>>(HttpErrorCode.InvalidInput, BidErrorCodes.OFFER_SUBMISSION_DATE_FROM_MUST_BE_EQUAL_TO_OR_BEFORE_OFFER_SUBMISSION_DATE_TO);

                var bids = _bidRepository.Find(x => x.CreatedBy == user.Id, false, nameof(Bid.BidAddressesTime), nameof(Bid.Association), nameof(Bid.BidMainClassificationMapping), nameof(Bid.BidStatus), nameof(Bid.BidType), nameof(Bid.BidRegions));

                if (!string.IsNullOrEmpty(request.RegionsId))
                {
                    var regionsIdAsString = request.RegionsId.Split(',');
                    int[] regionsId = Array.ConvertAll(regionsIdAsString, int.Parse);

                    bids = bids.Where(b => b.BidRegions.Select(x => regionsId.Contains(x.RegionId)).Any(x => x));
                }

                if (request.BidTypeId != null || request.BidTypeId > 0)
                    bids = bids.Where(a => a.BidTypeId == request.BidTypeId);
                if (request.BidMainClassificationId != null || request.BidMainClassificationId > 0)
                    bids = bids.Where(a => a.Bid_Industries.Where(bm => bm.CommercialSectorsTreeId == request.BidMainClassificationId).Count() > 0);
                if (!string.IsNullOrEmpty(request.Publisher))
                    bids = bids.Where(a => !string.IsNullOrEmpty(a.Presented_To) && a.Presented_To.Contains(request.Publisher));
                if (!string.IsNullOrEmpty(request.BiddingName))
                    bids = bids.Where(a => a.BidName.Contains(request.BiddingName));
                if (!string.IsNullOrEmpty(request.BiddingRefNumber))
                    bids = bids.Where(a => a.Ref_Number.Contains(request.BiddingRefNumber));
                if (!string.IsNullOrEmpty(request.BidsStatus) && request.BidsStatus == BidsStatus.Active.ToString())
                    bids = bids.Where(a => a.BidAddressesTime.LastDateInOffersSubmission > _dateTimeZone.CurrentDate);
                if (!string.IsNullOrEmpty(request.BidsStatus) && request.BidsStatus == BidsStatus.Expired.ToString())
                    bids = bids.Where(a => a.BidAddressesTime.LastDateInOffersSubmission < _dateTimeZone.CurrentDate);

                if (request.PublishDateFrom != null && request.PublishDateTo != null)
                    bids = bids.Where(a => a.BidAddressesTime.OffersOpeningDate >= request.PublishDateFrom && a.BidAddressesTime.OffersOpeningDate <= request.PublishDateTo);
                else if (request.PublishDateFrom != null && request.PublishDateTo == null)
                    bids = bids.Where(a => a.BidAddressesTime.OffersOpeningDate >= request.PublishDateFrom);
                else if (request.PublishDateFrom == null && request.PublishDateTo != null)
                    bids = bids.Where(a => a.BidAddressesTime.OffersOpeningDate <= request.PublishDateTo);

                if (request.OfferSubmissionDateFrom != null && request.OfferSubmissionDateTo != null)
                    bids = bids.Where(a => a.BidAddressesTime.LastDateInOffersSubmission >= request.OfferSubmissionDateFrom && a.BidAddressesTime.LastDateInOffersSubmission <= request.OfferSubmissionDateTo);
                else if (request.OfferSubmissionDateFrom != null && request.OfferSubmissionDateTo == null)
                    bids = bids.Where(a => a.BidAddressesTime.LastDateInOffersSubmission >= request.OfferSubmissionDateFrom);
                else if (request.OfferSubmissionDateFrom == null && request.OfferSubmissionDateTo != null)
                    bids = bids.Where(a => a.BidAddressesTime.LastDateInOffersSubmission <= request.OfferSubmissionDateTo);//RegionId

                if (request.TermsBookPriceId != null || request.TermsBookPriceId == (int)TermsBookPrice.Free)
                    bids = bids.Where(a => a.Bid_Documents_Price == 0);
                else if (request.TermsBookPriceId != null || request.TermsBookPriceId == (int)TermsBookPrice.p1_1000)
                    bids = bids.Where(a => a.Bid_Documents_Price >= 1 && a.Bid_Documents_Price <= 1000);
                else if (request.TermsBookPriceId != null || request.TermsBookPriceId == (int)TermsBookPrice.p1001_10000)
                    bids = bids.Where(a => a.Bid_Documents_Price >= 1001 && a.Bid_Documents_Price <= 10000);
                else if (request.TermsBookPriceId != null || request.TermsBookPriceId == (int)TermsBookPrice.p10001_20000)
                    bids = bids.Where(a => a.Bid_Documents_Price >= 10001 && a.Bid_Documents_Price <= 20000);
                else if (request.TermsBookPriceId != null || request.TermsBookPriceId == (int)TermsBookPrice.p20001_40000)
                    bids = bids.Where(a => a.Bid_Documents_Price >= 20001 && a.Bid_Documents_Price <= 40000);
                else if (request.TermsBookPriceId != null || request.TermsBookPriceId == (int)TermsBookPrice.p40001_50000)
                    bids = bids.Where(a => a.Bid_Documents_Price >= 40001 && a.Bid_Documents_Price <= 50000);
                else if (request.TermsBookPriceId != null || request.TermsBookPriceId == (int)TermsBookPrice.greater50000)
                    bids = bids.Where(a => a.Bid_Documents_Price > 50000);

                if (request.RegionId != null || request.RegionId > 0)
                    bids = bids.Where(a => a.Association.RegionId == request.RegionId);


                if (request.BidStatusId != null || request.BidStatusId > 0)
                    bids = bids.Where(a => a.BidStatusId == request.BidStatusId);

                if (bids == null | bids.Count() == 0)
                {
                    return new PagedResponse<IReadOnlyList<ReadOnlyFilterBidModel>>(null, request.pageNumber, request.pageSize);
                }
                int totalRecords = bids.Count();
                bids = bids.OrderByDescending(a => a.CreationDate).Skip((request.pageNumber - 1) * request.pageSize).Take(request.pageSize);


                var bidsModels = _mapper.Map<IReadOnlyList<ReadOnlyFilterBidModel>>(bids.ToList());

                foreach (var item in bidsModels)
                {
                    var bid = bids.FirstOrDefault(bid => bid.Id == item.Id);
                    if (bid != null)
                        item.Regions = BidRegion.getAllRegionsAsListOfIds(bid.BidRegions);

                    item.Tender_Brochure_Policies_UrlResponse = await _imageService.GetFileResponseEncrypted(item.Tender_Brochure_Policies_Url, item.Tender_Brochure_Policies_FileName);
                }
                //bidsModels.ToList().ForEach( async b => b.Tender_Brochure_Policies_UrlResponse = await imageService.GetFilePath(b.Tender_Brochure_Policies_Url)
                //!string.IsNullOrEmpty(b.Tender_Brochure_Policies_Url) ? fileSettings.BASE_URL + b.Tender_Brochure_Policies_Url : b.Tender_Brochure_Policies_Ur
                //l
                //);
                return new PagedResponse<IReadOnlyList<ReadOnlyFilterBidModel>>(bidsModels, request.pageNumber, request.pageSize, totalRecords);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = request,
                    ErrorMessage = "Failed to Get Bids Created By User!",
                    ControllerAndAction = "BidController/GetBidsCreatedByUser"
                });
                return new PagedResponse<IReadOnlyList<ReadOnlyFilterBidModel>>(
                       HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }

        public async Task<PagedResponse<IReadOnlyList<ReadOnlyFilterBidModel>>> GetAssociationBids(GetBidsCreatedByUserModel request)
        {
            try
            {
                var user = _currentUserService.CurrentUser;
                if (user is null)
                    return new PagedResponse<IReadOnlyList<ReadOnlyFilterBidModel>>(HttpErrorCode.NotAuthenticated);
                if (user == null && user.UserType != UserType.Association)
                {
                    return new PagedResponse<IReadOnlyList<ReadOnlyFilterBidModel>>(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);
                }
                if (request.PublishDateFrom > request.PublishDateTo)
                    return new PagedResponse<IReadOnlyList<ReadOnlyFilterBidModel>>(HttpErrorCode.InvalidInput, BidErrorCodes.PUBLISH_DATE_FROM_MUST_BE_EQUAL_TO_OR_BEFORE_PUBLISH_DATE_TO);
                if (request.OfferSubmissionDateFrom > request.OfferSubmissionDateTo)
                    return new PagedResponse<IReadOnlyList<ReadOnlyFilterBidModel>>(HttpErrorCode.InvalidInput, BidErrorCodes.OFFER_SUBMISSION_DATE_FROM_MUST_BE_EQUAL_TO_OR_BEFORE_OFFER_SUBMISSION_DATE_TO);

                long bidId = 0;
                var association = await _associationService.GetUserAssociation(user.Email);
                if (association != null)
                {
                    var bids = _bidRepository.Find(x => x.AssociationId == association.Id, false, nameof(Bid.BidAddressesTime), nameof(Bid.BidMainClassificationMapping), nameof(Bid.Association), nameof(Bid.BidStatus), nameof(Bid.BidType), nameof(Bid.BidRegions));//.Where(a => a.BidStatusId != (int)TenderStatus.Draft);

                    if (!string.IsNullOrEmpty(request.RegionsId))
                    {
                        var regionsIdAsString = request.RegionsId.Split(',');
                        int[] regionsId = Array.ConvertAll(regionsIdAsString, int.Parse);

                        bids = bids.Where(b => b.BidRegions.Select(x => regionsId.Contains(x.RegionId)).Any(x => x));
                    }

                    if (request.BidTypeId != null || request.BidTypeId > 0)
                        bids = bids.Where(a => a.BidTypeId == request.BidTypeId);
                    if (request.BidMainClassificationId != null || request.BidMainClassificationId > 0)
                        bids = bids.Where(a => a.Bid_Industries.Where(bm => bm.CommercialSectorsTreeId == request.BidMainClassificationId).Count() > 0);
                    if (!string.IsNullOrEmpty(request.Publisher))
                        bids = bids.Where(a => !string.IsNullOrEmpty(a.Presented_To) && a.Presented_To.Contains(request.Publisher));
                    if (!string.IsNullOrEmpty(request.BiddingName))
                        bids = bids.Where(a => a.BidName.Contains(request.BiddingName));
                    if (!string.IsNullOrEmpty(request.BiddingRefNumber))
                        bids = bids.Where(a => a.Ref_Number.Contains(request.BiddingRefNumber));
                    if (!string.IsNullOrEmpty(request.BidsStatus) && request.BidsStatus == BidsStatus.Active.ToString())
                        bids = bids.Where(a => a.BidAddressesTime.LastDateInOffersSubmission > _dateTimeZone.CurrentDate);
                    if (!string.IsNullOrEmpty(request.BidsStatus) && request.BidsStatus == BidsStatus.Expired.ToString())
                        bids = bids.Where(a => a.BidAddressesTime.LastDateInOffersSubmission < _dateTimeZone.CurrentDate);

                    if (request.PublishDateFrom != null && request.PublishDateTo != null)
                        bids = bids.Where(a => a.BidAddressesTime.OffersOpeningDate >= request.PublishDateFrom && a.BidAddressesTime.OffersOpeningDate <= request.PublishDateTo);
                    else if (request.PublishDateFrom != null && request.PublishDateTo == null)
                        bids = bids.Where(a => a.BidAddressesTime.OffersOpeningDate >= request.PublishDateFrom);
                    else if (request.PublishDateFrom == null && request.PublishDateTo != null)
                        bids = bids.Where(a => a.BidAddressesTime.OffersOpeningDate <= request.PublishDateTo);

                    if (request.OfferSubmissionDateFrom != null && request.OfferSubmissionDateTo != null)
                        bids = bids.Where(a => a.BidAddressesTime.LastDateInOffersSubmission >= request.OfferSubmissionDateFrom && a.BidAddressesTime.LastDateInOffersSubmission <= request.OfferSubmissionDateTo);
                    else if (request.OfferSubmissionDateFrom != null && request.OfferSubmissionDateTo == null)
                        bids = bids.Where(a => a.BidAddressesTime.LastDateInOffersSubmission >= request.OfferSubmissionDateFrom);
                    else if (request.OfferSubmissionDateFrom == null && request.OfferSubmissionDateTo != null)
                        bids = bids.Where(a => a.BidAddressesTime.LastDateInOffersSubmission <= request.OfferSubmissionDateTo);//RegionId

                    if (request.TermsBookPriceId != null && request.TermsBookPriceId == (int)TermsBookPrice.Free)
                        bids = bids.Where(a => a.Bid_Documents_Price == 0);
                    else if (request.TermsBookPriceId != null && request.TermsBookPriceId == (int)TermsBookPrice.p1_1000)
                        bids = bids.Where(a => a.Bid_Documents_Price >= 1 && a.Bid_Documents_Price <= 1000);
                    else if (request.TermsBookPriceId != null && request.TermsBookPriceId == (int)TermsBookPrice.p1001_10000)
                        bids = bids.Where(a => a.Bid_Documents_Price >= 1001 && a.Bid_Documents_Price <= 10000);
                    else if (request.TermsBookPriceId != null && request.TermsBookPriceId == (int)TermsBookPrice.p10001_20000)
                        bids = bids.Where(a => a.Bid_Documents_Price >= 10001 && a.Bid_Documents_Price <= 20000);
                    else if (request.TermsBookPriceId != null && request.TermsBookPriceId == (int)TermsBookPrice.p20001_40000)
                        bids = bids.Where(a => a.Bid_Documents_Price >= 20001 && a.Bid_Documents_Price <= 40000);
                    else if (request.TermsBookPriceId != null && request.TermsBookPriceId == (int)TermsBookPrice.p40001_50000)
                        bids = bids.Where(a => a.Bid_Documents_Price >= 40001 && a.Bid_Documents_Price <= 50000);
                    else if (request.TermsBookPriceId != null && request.TermsBookPriceId == (int)TermsBookPrice.greater50000)
                        bids = bids.Where(a => a.Bid_Documents_Price > 50000);

                    if (request.RegionId != null || request.RegionId > 0)
                        bids = bids.Where(a => a.Association.RegionId == request.RegionId);


                    if (request.BidStatusId != null || request.BidStatusId > 0)
                        bids = bids.Where(a => a.BidStatusId == request.BidStatusId);

                    if (bids == null || bids.Count() == 0)
                    {
                        return new PagedResponse<IReadOnlyList<ReadOnlyFilterBidModel>>(null, request.pageNumber, request.pageSize);
                    }
                    int totalRecords = bids.Count();
                    bids = bids.OrderByDescending(a => a.CreationDate).Skip((request.pageNumber - 1) * request.pageSize).Take(request.pageSize);

                    var bidsModels = _mapper.Map<IReadOnlyList<ReadOnlyFilterBidModel>>(bids.ToList());


                    if (user != null)
                    {
                        var userFavBids = await _userFavBidList.Find(x => x.UserId == user.Id).ToListAsync();

                        foreach (var itm in bidsModels)
                        {
                            if (userFavBids.FirstOrDefault(a => a.BidId == itm.Id) != null)
                                itm.IsUserFavorite = true;

                            //added Flag(IsBidAwarded)-->to know if bid is awarded or not 
                            var awardingSelection = _awardingSelectRepository.Find(x => x.BidId == itm.Id).ToList();
                            itm.IsBidAwarded = awardingSelection.Count() > 0 ? true : false;

                            var tenderCount = await _tenderSubmitQuotationRepository.FindAsync(x => x.BidId == itm.Id && x.ProposalStatus == ProposalStatus.Delivered, true);
                            itm.TenderQuotationsCount = tenderCount.Count();
                        }
                    }
                    foreach (var item in bidsModels)
                    {
                        var bid = bids.FirstOrDefault(bid => bid.Id == item.Id);
                        if (bid != null)
                            item.Regions = BidRegion.getAllRegionsAsListOfIds(bid.BidRegions);

                        item.Tender_Brochure_Policies_UrlResponse = await _imageService.GetFileResponseEncrypted(item.Tender_Brochure_Policies_Url, item.Tender_Brochure_Policies_FileName);
                        item.Association.ImageResponse = await _imageService.GetFileResponseEncrypted(item.Association.Image, item.Association.ImageFileName);
                        item.IsCurrentAssociation = true;
                    }

                    //bidsModels.ToList().ForEach(async b => {   b.Tender_Brochure_Policies_UrlResponse =  await imageService.GetFilePath(b.Tender_Brochure_Policies_Url);
                    //    b.Association.ImageResponse = await imageService.GetFilePath(b.Association.Image);
                    //    b.IsCurrentAssociation = true; });
                    return new PagedResponse<IReadOnlyList<ReadOnlyFilterBidModel>>(bidsModels, request.pageNumber, request.pageSize, totalRecords);
                }
                return new PagedResponse<IReadOnlyList<ReadOnlyFilterBidModel>>(
                              HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = request,
                    ErrorMessage = "Failed to Get Association Bid!",
                    ControllerAndAction = "BidController/GetAssociationBids"
                });
                return new PagedResponse<IReadOnlyList<ReadOnlyFilterBidModel>>(
                       HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }

        public async Task<OperationResult<GetUserRoleResponse>> GetUserRole()
        {
            try
            {
                var usr = _currentUserService.CurrentUser;
                if (usr == null || usr.UserType != UserType.Association /*&& usr.UserType != UserType.SuperAdmin*/)
                {
                    return OperationResult<GetUserRoleResponse>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);
                }
                var association = await _associationService.GetUserAssociation(usr.Email);
                if (association == null)
                    return OperationResult<GetUserRoleResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.ASSOCIATION_NOT_FOUND);

                if (usr.Email.ToLower() == association.Manager_Email.ToLower())
                    return OperationResult<GetUserRoleResponse>.Success(new GetUserRoleResponse { Role = "Manager" });
                if (usr.Email.ToLower() == association.Email.ToLower() || (await _associationAdditional_ContactRepository.FindOneAsync(a => a.Email.ToLower() == usr.Email.ToLower())) != null)
                    return OperationResult<GetUserRoleResponse>.Success(new GetUserRoleResponse { Role = "Creator" });
                return OperationResult<GetUserRoleResponse>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = null,
                    ErrorMessage = "Failed to Get user role!",
                    ControllerAndAction = "BidController/GetUserRole"
                });
                return OperationResult<GetUserRoleResponse>.Fail(
                       HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }

        public async Task<OperationResult<long>> AddRFIandRequests(AddRFIRequestModel model)
        {
            var usr = _currentUserService.CurrentUser;
            if (usr == null && usr.UserType == UserType.Association)
            {
                return OperationResult<long>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);
            }

            try
            {
                long rFIRequestId = 0;
                var bid = await _bidRepository.FindOneAsync(x => x.Id == model.BidId, false);

                if (bid == null)
                {
                    return OperationResult<long>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.INVALID_BID);
                }

                if (model.Id != 0)
                {
                    var rFIRequest = await _rFIRequestRepository.FindOneAsync(x => x.Id == model.Id, false);

                    if (rFIRequest == null)
                    {
                        return OperationResult<long>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.INVALID_RFI_REQUEST);
                    }

                    rFIRequest.Subject = model.Subject;
                    rFIRequest.Details = model.Details;
                    rFIRequest.TypeId = model.TypeId;
                    rFIRequest.BidId = model.BidId;
                    rFIRequest.ModifiedBy = usr.Id;
                    rFIRequest.ModificationDate = _dateTimeZone.CurrentDate;

                    await _rFIRequestRepository.Update(rFIRequest);
                }
                else
                {
                    var entity = _mapper.Map<RFIRequest>(model);
                    entity.IsDeleted = false;
                    entity.CreatedBy = usr.Id;

                    await _rFIRequestRepository.Add(entity);
                    rFIRequestId = entity.Id;
                }

                return OperationResult<long>.Success(rFIRequestId);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = model,
                    ErrorMessage = "Failed to Add Bid RFi/Request!",
                    ControllerAndAction = "BidController/AddRFIandRequests"
                });
                return OperationResult<long>.Fail(
                       HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }
        public async Task<OperationResult<IReadOnlyList<ReadOnlyBidRFiRequestModel>>> GetBidRFiRequests(long bidId, int typeId)
        {
            try
            {
                var rFIRequests = _rFIRequestRepository.Find(x => x.BidId == bidId, false).Where(a => a.TypeId == typeId);


                if (rFIRequests == null)
                {
                    return OperationResult<IReadOnlyList<ReadOnlyBidRFiRequestModel>>.Success(null);
                }
                var model = _mapper.Map<IReadOnlyList<ReadOnlyBidRFiRequestModel>>(rFIRequests);

                return OperationResult<IReadOnlyList<ReadOnlyBidRFiRequestModel>>.Success(model);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = $"bidId = {bidId} &  typeId = {typeId}",
                    ErrorMessage = "Failed to get RFi / Requests!",
                    ControllerAndAction = "BidController/GetBidRFiRequests"
                });
                return OperationResult<IReadOnlyList<ReadOnlyBidRFiRequestModel>>.Fail(
                       HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }

        //commented
        public async Task<OperationResult<ReadOnlyBidStatusDetailsModel>> GetBidStatusDetails(long bidId)
        {
            try
            {
                var user = _currentUserService.CurrentUser;
                if (user == null)
                {
                    return OperationResult<ReadOnlyBidStatusDetailsModel>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);
                }
                var bid = await _bidRepository.FindOneAsync(x => x.Id == bidId, false, nameof(Bid.BidStatus), nameof(Bid.BidAddressesTime), nameof(Bid.BidType));

                if (bid == null)
                    return OperationResult<ReadOnlyBidStatusDetailsModel>.Success(null);

                long currentAssociationId = 0;
                if (user.UserType == UserType.Association)
                {
                    var association = await _associationService.GetUserAssociation(user.Email);
                    if (association != null)
                        currentAssociationId = association.Id;
                }
                bool IsCurrentAssociation = bid.AssociationId == currentAssociationId ? true : false;
                var model = _mapper.Map<ReadOnlyBidStatusDetailsModel>(bid);
                //model.ToList().ForEach(x => x. = !string.IsNullOrEmpty(x.AttachedFileURL) ? fileSettings.BASE_URL + x.AttachedFileURL : x.AttachedFileURL);
                model.BidVisibilityName = bid.BidType.NameAr;
                model.IsCurrentAssociation = IsCurrentAssociation;
                model.ExpectedAnchoringDate = bid.BidAddressesTime?.ExpectedAnchoringDate;
                return OperationResult<ReadOnlyBidStatusDetailsModel>.Success(model);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = $"bidId = {bidId}",
                    ErrorMessage = "Failed to get bid status details!",
                    ControllerAndAction = "BidController/GetBidStatusDetails"
                });
                return OperationResult<ReadOnlyBidStatusDetailsModel>.Fail(
                       HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }

        //4
        public async Task<OperationResult<ReadOnlyGetBidPriceModel>> GetBidPrice(GetBidDocumentsPriceRequestModel request)
        {
            try
            {
                {
                    if (_currentUserService.IsUserNotAuthorized(new List<UserType> { UserType.Provider, UserType.Freelancer }))
                        return OperationResult<ReadOnlyGetBidPriceModel>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);
                    var user = _currentUserService.CurrentUser;

                    OperationResult<ReadOnlyGetBidPriceModel> result = null;
                    if (user.UserType == UserType.Provider)
                    {
                        if ((!string.IsNullOrEmpty(request.CouponHash) && request.AddonsId is not null))
                            return OperationResult<ReadOnlyGetBidPriceModel>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.INVALID_INPUT);

                        //Ask Eng/Faten
                        if (request.CompanyId is not null)
                        {
                            var verifyResult = await VerifyCommercialRecordIfAutomatedRegistration(user,request.CompanyId);
                            if (!verifyResult.IsSucceeded)
                                return verifyResult;
                        }

                        var bid = await _bidRepository.FindOneAsync(x => x.Id == request.BidId, false, nameof(Bid.Association), nameof(Bid.Bid_Industries), nameof(Bid.BidRegions));
                        if (bid is null)
                            return OperationResult<ReadOnlyGetBidPriceModel>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);

                        var company = await _companyRepository.FindOneAsync(x => x.Id == request.CompanyId && user.OrgnizationType == (int)OrganizationType.Comapny, false);
                        if (company is null && request.CompanyId is not null)
                            return OperationResult<ReadOnlyGetBidPriceModel>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.COMPANY_NOT_FOUND);
                        var freelancer = await _freelancerRepository.FindOneAsync(x => x.Id == request.FreelancerId && user.OrgnizationType == (int)OrganizationType.Freelancer, false);
                        if (freelancer is null && request.FreelancerId is not null)
                            return OperationResult<ReadOnlyGetBidPriceModel>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.FREELANCER_NOT_FOUND);
                        if (request.AddonsId is not null)
                        {
                            var validationResponse = await _subscriptionAddonsService.ValidateAddOnForUser(new VaildateAddOnForUser
                            {
                                UserType = user.UserType == UserType.Provider ? UserType.Company : user.UserType,
                                UserId = company?.Id ?? freelancer?.Id ?? 0,
                                BidId = bid.Id,
                                AddOnId = request.AddonsId ?? 0,
                            });

                            if (!validationResponse.IsSucceeded)
                                return OperationResult<ReadOnlyGetBidPriceModel>.Fail(validationResponse.HttpErrorCode, validationResponse.Code, validationResponse.ErrorMessage);

                            return await _subscriptionAddonsService.GetDocumentPrice(new GetDocumentPricAddOnForUser
                            {
                                bid = bid,
                                UserType = user.UserType == UserType.Provider ? UserType.Company : user.UserType,
                                UserId = company?.Id ?? freelancer?.Id ?? 0,
                                AddOnId = request.AddonsId ?? 0
                            });
                        }
                        else if (!string.IsNullOrEmpty(request.CouponHash))
                        {
                            var couponValidationResult = await _bidAndCouponServicesCommonMethods.CheckIfCouponIsValidAsync(new CheckIfCouponIsValidRequestModel
                            {
                                CouponHash = request.CouponHash,
                                CouponType = CouponType.BidDocs,
                                UserId = company?.Id ?? freelancer?.Id ?? 0,
                                UserType = user.UserType == UserType.Provider ? UserType.Company : user.UserType,
                                Price = bid.Association_Fees + bid.Tanafos_Fees,
                            });
                            if (!couponValidationResult.IsSucceeded)
                                return OperationResult<ReadOnlyGetBidPriceModel>.Fail(HttpErrorCode.InvalidInput, couponValidationResult.Code);

                            var couponOfBidValidationResult = await _bidAndCouponServicesCommonMethods.CheckIfCouponOfBidIsValidAsync(request.CouponHash, bid);
                            if (!couponOfBidValidationResult.IsSucceeded)
                                return OperationResult<ReadOnlyGetBidPriceModel>.Fail(HttpErrorCode.InvalidInput,couponOfBidValidationResult.Code);
                        }

                        result = await _bidAndCouponServicesCommonMethods.GetBidDocumentsPrice(request.CouponHash, bid);
                    }

                    else if (user.UserType == UserType.Freelancer)
                        result = await GetBidPriceForFreelancer(request);

                    return result;
                }
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = request,
                    ErrorMessage = "Failed to get bid Price!",
                    ControllerAndAction = "BidController/GetBidPrice"
                });
                return OperationResult<ReadOnlyGetBidPriceModel>.Fail(HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }
        public async Task<OperationResult<ReadOnlyGetBidPriceModel>> GetBidPriceForFreelancer(GetBidDocumentsPriceRequestModel request)
        {
            try
            {
                {
                    if (_currentUserService.IsUserNotAuthorized(new List<UserType> { UserType.Freelancer }))
                        return OperationResult<ReadOnlyGetBidPriceModel>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);
                    var user = _currentUserService.CurrentUser;

                    if ((!string.IsNullOrEmpty(request.CouponHash) && request.AddonsId is not null))
                        return OperationResult<ReadOnlyGetBidPriceModel>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.INVALID_INPUT);

                    var bid = await _bidRepository.FindOneAsync(x => x.Id == request.BidId, false, nameof(Bid.Association), nameof(Bid.FreelanceBidIndustries), nameof(Bid.BidRegions));
                    if (bid is null)
                        return OperationResult<ReadOnlyGetBidPriceModel>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);

                    var freelancer = await _freelancerRepository.FindOneAsync(x => x.Id == request.FreelancerId && user.OrgnizationType == (int)OrganizationType.Freelancer, false);
                    if (freelancer is null)
                        return OperationResult<ReadOnlyGetBidPriceModel>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.FREELANCER_NOT_FOUND);

                    if (request.AddonsId is not null)
                    {
                        var validationResponse = await _subscriptionAddonsService.ValidateAddOnForUser(new VaildateAddOnForUser
                        {
                            UserType = UserType.Freelancer,
                            UserId = freelancer.Id,
                            BidId = bid.Id,
                            AddOnId = request.AddonsId ?? 0,
                        });

                        if (!validationResponse.IsSucceeded)
                            return OperationResult<ReadOnlyGetBidPriceModel>.Fail(validationResponse.HttpErrorCode, validationResponse.Code, validationResponse.ErrorMessage);

                        return await _subscriptionAddonsService.GetDocumentPrice(new GetDocumentPricAddOnForUser
                        {
                            bid = bid,
                            UserType = UserType.Freelancer,
                            UserId = freelancer.Id,
                            AddOnId = request.AddonsId ?? 0
                        });
                    }
                    else if (!string.IsNullOrEmpty(request.CouponHash))
                    {
                        var couponValidationResult = await _bidAndCouponServicesCommonMethods.CheckIfCouponIsValidAsync(new CheckIfCouponIsValidRequestModel
                        {
                            CouponHash = request.CouponHash,
                            CouponType = CouponType.BidDocs,
                            UserId = freelancer.Id,
                            UserType = UserType.Freelancer,
                            Price = bid.Association_Fees + bid.Tanafos_Fees,
                        });
                        if (!couponValidationResult.IsSucceeded)
                            return OperationResult<ReadOnlyGetBidPriceModel>.Fail(HttpErrorCode.InvalidInput, couponValidationResult.Code);

                        var couponOfBidValidationResult = await _bidAndCouponServicesCommonMethods.CheckIfCouponOfBidIsValidAsync(request.CouponHash, bid);
                        if (!couponOfBidValidationResult.IsSucceeded)
                            return OperationResult<ReadOnlyGetBidPriceModel>.Fail(HttpErrorCode.InvalidInput,couponOfBidValidationResult.Code);
                    }

                    return await _bidAndCouponServicesCommonMethods.GetBidDocumentsPrice(request.CouponHash, bid);
                }
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = request,
                    ErrorMessage = "Failed to get bid Price!",
                    ControllerAndAction = "BidController/GetBidPrice"
                });
                return OperationResult<ReadOnlyGetBidPriceModel>.Fail(HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }

        private async Task<OperationResult<ReadOnlyGetBidPriceModel>> VerifyCommercialRecordIfAutomatedRegistration(ApplicationUser usr, long? CompanyId)
        {
            var integrativeService = await _integrativeServicesRepository.FindOneAsync(x => true);
            if (integrativeService is not null && integrativeService.AutomatedProviderRegistration)
            {

                var company = await _companyRepository.Find(x => x.Id == CompanyId)
                    .FirstOrDefaultAsync();

                if(company is null)
                    return OperationResult<ReadOnlyGetBidPriceModel>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.COMPANY_NOT_FOUND);

                if (string.IsNullOrEmpty(company.UniqueNumber700) || company.EstablishmentStatusId != (int)EstablishmentStatus.Active)
                    return OperationResult<ReadOnlyGetBidPriceModel>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.CR_NATIONAL_NUMBER_CAN_NOT_BE_NULL);

                var validationResult = await _companyNICService.CheckIfCommercialRecordIsValid(company.UniqueNumber700);
                if (!validationResult.IsSucceeded || !validationResult.Data.IsValid || validationResult.Data.Status is null)
                    return OperationResult<ReadOnlyGetBidPriceModel>.Fail(HttpErrorCode.Conflict, validationResult.Code);


            }
            return OperationResult<ReadOnlyGetBidPriceModel>.Success(null);
        }



        public async Task<OperationResult<BuyTermsBookResponseModel>> BuyTermsBook(BuyTermsBookModel model)
        {
            try
            {
                var usr = _currentUserService.CurrentUser;
                if (_currentUserService.IsUserNotAuthorized(new List<UserType> { UserType.Provider, UserType.Freelancer }))
                    return OperationResult<BuyTermsBookResponseModel>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);

                var bid = await _bidRepository.Find(x => x.Id == model.BidId && x.BidStatusId == (int)TenderStatus.Open,
                    false, nameof(Bid.TenderSubmitQuotation))
                    .IncludeBasicBidData()
                    .FirstOrDefaultAsync();
                var numOftendersOffers = await _tenderSubmitQuotationRepository.FindAsync(x => x.BidId == bid.Id && x.ProposalStatus == ProposalStatus.Delivered);
                if (bid.isLimitedOffers == true && numOftendersOffers.Count() >= bid.limitedOffers)
                    return OperationResult<BuyTermsBookResponseModel>.Fail(HttpErrorCode.InvalidInput, CommonErrorCodes.BID_IS_LIMITED_OFFERS);

                bool isCompanyBuyTermsBook = model.CompanyId is not null;
                GetCompaniesToBuyTermsBookResponse company = null;
                GetFreelancersToBuyTermsBookResponse freelancer = null;

                if (isCompanyBuyTermsBook)
                {
                    var canCompanyBuyTermsBookValidationResult = await CanCompanyBuyTermsBook(bid, model.CompanyId);
                    if (!canCompanyBuyTermsBookValidationResult.IsSucceeded)
                        return OperationResult<BuyTermsBookResponseModel>.FailFrom(canCompanyBuyTermsBookValidationResult);

                    company = canCompanyBuyTermsBookValidationResult.Data;
                }
                else if (!isCompanyBuyTermsBook && model.FreelancerId is not null)
                {
                    var canFreelancerBuyTermsBookValidationResult = await CanFreelancerBuyTermsBook(bid, model.FreelancerId);

                    if (!canFreelancerBuyTermsBookValidationResult.IsSucceeded)
                        return OperationResult<BuyTermsBookResponseModel>.Fail(canFreelancerBuyTermsBookValidationResult.HttpErrorCode, canFreelancerBuyTermsBookValidationResult.Code, canFreelancerBuyTermsBookValidationResult.ErrorMessage);

                    freelancer = canFreelancerBuyTermsBookValidationResult.Data;
                }

                Donor donor = await _donorRepository.FindOneAsync(x => x.Id == bid.EntityId);
                if (donor is null && bid.EntityType == UserType.Donor)
                    return OperationResult<BuyTermsBookResponseModel>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.INVALID_BID);

                if (!string.IsNullOrEmpty(model.CouponHash) && model.AddonsId != null)
                    return OperationResult<BuyTermsBookResponseModel>.Fail(HttpErrorCode.InvalidInput, CommonErrorCodes.YOU_CAN_NOT_USE_ADD_ON_WITH_A_COUPON);

                var bidPricesResult = isCompanyBuyTermsBook ? await GetBidPrice(new GetBidDocumentsPriceRequestModel
                {
                    BidId = model.BidId,
                    AddonsId = model.AddonsId,
                    CouponHash = model.CouponHash,
                    CompanyId = model.CompanyId,

                }) : await GetBidPriceForFreelancer(new GetBidDocumentsPriceRequestModel
                {
                    BidId = model.BidId,
                    AddonsId = model.AddonsId,
                    CouponHash = model.CouponHash,
                    FreelancerId = model.FreelancerId
                });
                if (!bidPricesResult.IsSucceeded)
                    return OperationResult<BuyTermsBookResponseModel>.Fail(HttpErrorCode.InvalidInput, bidPricesResult.Code);

                var bidPrices = bidPricesResult.Data;

                if (!string.IsNullOrEmpty(model.CouponHash))
                {
                    var reservationResult = await _helperService.ReserveCoupon(model.CouponHash);
                    if (!reservationResult.IsSucceeded)
                    {
                        _logger.Log(new LoggerModel
                        {
                            ExceptionError = null,
                            UserRequestModel = null,
                            ErrorMessage = $"Coupon reservationResult.IsNotSucceeded",
                            ControllerAndAction = $"{nameof(BidService)}/{nameof(BuyTermsBook)}"
                        });
                    }
                }

                var currentSubscriptionAddOn = await _subscriptionPaymentRepository.Find(x => x.UserId == usr.CurrentOrgnizationId && ((model.CompanyId != null && x.UserTypeId == UserType.Company) || (model.FreelancerId != null && x.UserTypeId == UserType.Freelancer)) && x.IsPaymentConfirmed && !x.IsExpired
                                                                                           && x.SubscriptionAddOns.Any(a => a.AddOnId == model.AddonsId && !a.IsExpired && a.IsPaymentConfirmed), true, false)
                    .OrderByDescending(x => x.CreationDate)
                    .Select(x => new
                    {
                        Id = x.SubscriptionAddOns.FirstOrDefault(a => a.AddOnId == model.AddonsId && !a.IsExpired && a.IsPaymentConfirmed).Id,
                    })
                    .FirstOrDefaultAsync();

                if (model.AddonsId.HasValue && currentSubscriptionAddOn is null)
                    return OperationResult<BuyTermsBookResponseModel>.Fail(HttpErrorCode.InvalidInput, CommonErrorCodes.SELECTED_ADD_ON_IS_WRONG);

                var paymentGatewaySettings = await _paymentGatewaySettingRepository.FindOneAsync(x => x.IsActive);
                var generalSettings = await _appGeneralSettingsRepository.FindOneAsync(x => true);

                var entity = _mapper.Map<ProviderBid>(model);
                entity.CouponHash = bidPrices.CouponHash;
                entity.SubscriptionAddonId = model.AddonsId.HasValue ? currentSubscriptionAddOn.Id : null;

                entity.DiscountType = model.AddonsId.HasValue ? DiscountType.Addon : (bidPrices.CouponHash != null ? DiscountType.Coupon : null);
                entity.Price = (double)bidPrices.Bid_Documents_Price;
                entity.TanafosTaxesAmount = (double)bidPrices.TanafosTaxAfterDiscount;
                entity.TanafosFeesAfterDiscount = (double)bidPrices.TanafosFeesAfterDiscount;
                entity.AssociationFeesAfterDiscount = (double)bidPrices.AssociationFeesAfterDiscount;
                entity.TransactionNumber = await GenerateTransactioNumber();
                entity.CompanyId = company?.Id;
                entity.FreelancerId = freelancer?.Id;
                entity.CreatedBy = usr.Id;
                entity.PaymentMethodId = ((int)paymentGatewaySettings.PaymentMethod);
                entity.IsPaymentConfirmed = entity.Price == 0;
                entity.IsRefunded = false;
                entity.InvoiceId = null;
                entity.UserType = usr.UserType == UserType.Provider ? UserType.Company : usr.UserType;
                if (bid.EntityType == UserType.Association)
                    MapProviderBidTaxes(generalSettings, bid.Association.HasTaxRecordNumber, entity, bid);
                else if (bid.EntityType == UserType.Donor)
                    MapProviderBidTaxes(generalSettings, donor.HasTaxRecordNumber, entity, bid);

                await _providerBidRepository.AddThenDeAttach(entity);
      

                #region Is Free  
                if (entity.IsPaymentConfirmed)
                {
                    entity.InvoiceId = await _invoiceService.GenerateBuyTenderDocInvoiceId();
                    await _providerBidRepository.Update(entity);

                    var invoicePdfResult = await _invoiceService.GenerateAndSaveBuyTenderDocInvoicePDF(bid, entity);
                    if (!invoicePdfResult.IsSucceeded)
                        return OperationResult<BuyTermsBookResponseModel>.Fail(invoicePdfResult.HttpErrorCode, invoicePdfResult.Code, invoicePdfResult.ErrorMessage);

                    entity.SystemGeneratedInvoiceFileName = invoicePdfResult.Data.FileName;
                    entity.SystemGeneratedInvoiceFilePath = invoicePdfResult.Data.FilePath;
                    await _providerBidRepository.Update(entity);

                    await LogBuyTenderTermsBookEvent(bid, entity, company?.Name ?? freelancer?.Name);
                    //TODO
                    await _pointEventService.AddPointEventUsageHistoryAsync(new AddPointEventUsageHistoryModel
                    {
                        PointType = PointTypes.BuyBidTermsBook,
                        ActionId = bid.Id,
                    });

                    if (!string.IsNullOrEmpty(entity.CouponHash))
                    {
                        entity = await ApplyCouponIfExist(entity);
                    }
                    else if (entity.SubscriptionAddonId.HasValue)
                    {
                        var updateSubscripedAddOnResult = await _subscriptionAddonsService.UpdateAddOnUserAndAddUsageHistory(new UpdateAddOnAndAddUsageHistoryModel
                        {
                            EntityId = company?.Id ?? freelancer.Id,
                            UserType = company != null ? UserType.Company : UserType.Freelancer,
                            SubscriptionAddonId = entity.SubscriptionAddonId.Value,

                            TotalBeforeDiscount = bidPrices.BidDocumentsPriceWithoutVatBeforeDiscount,
                            DiscountQuantity = bidPrices.Discount,
                            TotalAfterDiscount = bidPrices.Bid_Documents_Price,
                            VatAmountAfterDiscount = bidPrices.TotalVat,
                        });
                        if (!updateSubscripedAddOnResult.IsSucceeded)
                            return OperationResult<BuyTermsBookResponseModel>.Fail(updateSubscripedAddOnResult.HttpErrorCode, updateSubscripedAddOnResult.Code, updateSubscripedAddOnResult.ErrorMessage);

                        entity.AddOnUsagesHistoryId = updateSubscripedAddOnResult.Data.Id;
                        await _providerBidRepository.Update(entity);
                    }

                    if ((model.CompanyId != null || model.FreelancerId != null) && bid.IsApplyOfferWithSubscriptionMandatory == true)
                    {
                        var quotation = await _tenderSubmitQuotationRepository.FindOneAsync(x => x.BidId == entity.BidId
                                && (x.CompanyId == entity.CompanyId || x.FreelancerId == entity.FreelancerId)
                                && x.ProposalStatus == ProposalStatus.NotPaid, false, nameof(TenderSubmitQuotation.Company), nameof(TenderSubmitQuotation.Freelancer));
                        if (quotation != null)
                        {
                            quotation.ProposalStatus = ProposalStatus.Delivered;
                            await _tenderSubmitQuotationRepository.Update(quotation);
                        }
                        await _helperService.SendOfferEmailsAndNotifications(usr.Id, bid, quotation.Company, quotation, quotation.Freelancer);
                    }

                    var bidProviderEntityId = entity.CompanyId.HasValue ? entity.CompanyId.Value : entity.FreelancerId.Value;
                    await _helperService.RevealBidAfterBuyingTermsBookIfAllowedAsync(entity.BidId, bidProviderEntityId, entity.UserType, BidRevealStatus.RevealedViaBuyTermsBook);

                    using var scope = _serviceProvider.CreateScope();
                    var financialTransactionService = scope.ServiceProvider.GetRequiredService<IFinancialTransactionPartiesPercentageService>();
                    await financialTransactionService.SetFinancialTransactionPartiesPercentage(new SetFinancialTransactionPartiesPercentageRequest()
                    {
                        ServiceId = entity.Id,
                        TransactionType = FinancialTransaction.BuyTermsPolicy,
                        GatewayFees = 0,
                        CardUsed = CardUsed.Free,
                    });

                    //TODO + Script + FreelanceBidIndustries
                    await _channelWriterTenderDocs.WriteAsync(new GenerateTenderDocsPillModel() { bidProvider = entity, GeneralSettings = generalSettings, File = new FileResponse { FilePath = entity.SystemGeneratedInvoiceFilePath, FileName = entity.SystemGeneratedInvoiceFileName } });
                    await _commonEmailAndNotificationService.SenfEmailToBidCreatorAndSuperAdminAndAdminsAfterBuyingTenderTerms(entity.BidId, company?.Name ?? freelancer.Name);

                    var responseModel = new BuyTermsBookResponseModel
                    {
                        PaymentUrl = "",
                        TransactionNumber = entity.TransactionNumber,
                        PaymentEventResponse = await _helperService.GetBuyTermsBookPaymentResponse(entity.TransactionNumber, entity.CompanyId != null),
                    };
                    return OperationResult<BuyTermsBookResponseModel>.Success(responseModel);
                }
                #endregion

                #region Payment
                var paymentRequest = new PaymentRequestDto
                {
                    Amount = entity.Price,
                    Currency = "SAR",
                    Descriptions = PaymentServiceType.BUY_TERMS_BOOK.AsName(),
                    transactionId = entity.TransactionNumber,
                    BidId = model.BidId,
                    CompanyId = company?.Id,
                    PaymentServiceType = PaymentServiceType.BUY_TERMS_BOOK,
                    EntityId = company != null ? company.Id : freelancer.Id,
                    EntityType = company != null ? UserType.Company : UserType.Freelancer,
                    HyperPayEntityIdType = model.EntityIdType
                };
                var result = await (await _paymentGatewayFactory.GetActivePaymentGateway()).GetPaymentUrl(paymentRequest);

                var response = new BuyTermsBookResponseModel
                {
                    PaymentUrl = result.Data,
                    TransactionNumber = entity.TransactionNumber
                };
                return OperationResult<BuyTermsBookResponseModel>.Success(response);
                #endregion
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = model,
                    ErrorMessage = "Failed to Buy Terms Book!",
                    ControllerAndAction = "BidController/BuyTermsBook"
                });
                if (!string.IsNullOrEmpty(model.CouponHash))
                {
                    try
                    {
                        await _helperService.ReleaseCouponReservation(model.CouponHash, $"Exception: {ex.Message}");
                    }
                    catch
                    {
                    }
                }
                return OperationResult<BuyTermsBookResponseModel>.Fail(
                       HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }

        private async Task<string> GenerateTransactioNumber()
        {
            string nextNumber = "000001";
            string randomNumber = _invoiceService.GenerateBaseInvoiceNumber(InvoiceType.BuyTenderDocsInvoice);
            var getMaxTransactionNumber = await _providerBidRepository
                                        .Find(x => x.TransactionNumber.ToLower().Length > 12)
                                        .OrderByDescending(x => x.CreationDate)
                                        .FirstOrDefaultAsync();
            if (getMaxTransactionNumber is null)
            {
                randomNumber = randomNumber + nextNumber;
            }
            else
            {
                nextNumber = getMaxTransactionNumber.TransactionNumber.Substring(7);
                nextNumber = (Convert.ToInt32(nextNumber) + 1).ToString();
                nextNumber = nextNumber.PadLeft(6, '0');

                randomNumber = randomNumber + nextNumber;
            }

            var checkTransactionNumber = await _providerBidRepository.FindOneAsync(x =>
                          (string.Equals(x.TransactionNumber.ToLower(), randomNumber)));
            do
            {
                if (checkTransactionNumber != null)
                {
                    nextNumber = (Convert.ToInt32(nextNumber) + 1).ToString();
                    nextNumber = nextNumber.PadLeft(6, '0');

                    randomNumber = randomNumber + nextNumber;
                    checkTransactionNumber = await _providerBidRepository.FindOneAsync(x =>
                                       (string.Equals(x.TransactionNumber.ToLower(), randomNumber)));
                }
            } while (checkTransactionNumber != null);

            return randomNumber;
        }

        private async Task<OperationResult<GetCompaniesToBuyTermsBookResponse>> CanCompanyBuyTermsBook(Bid bid, long? companyId)
        {
            if (bid is null || bid.BidTypeId == (int)BidTypes.Freelancing)
                return OperationResult<GetCompaniesToBuyTermsBookResponse>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.INVALID_BID);

            if (!bid.IsAbleToSubscribeToBid)
                return OperationResult<GetCompaniesToBuyTermsBookResponse>.Fail(HttpErrorCode.InvalidInput, CommonErrorCodes.SUBSCRIPTION_IN_THIS_BID_HAS_BEEN_SUSPENDED);

            if (bid.BidTypeId != (int)BidTypes.Instant && ((DateTime)bid.BidAddressesTime.LastDateInOffersSubmission).Date < _dateTimeZone.CurrentDate.Date)
                return OperationResult<GetCompaniesToBuyTermsBookResponse>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.DATE_IS_GREATER_THAN_LAST_DATE_IN_OFFERS_SUBMISSION);

            if (bid.BidStatusId != (int)TenderStatus.Open)
                return OperationResult<GetCompaniesToBuyTermsBookResponse>.Fail(HttpErrorCode.InvalidInput, CommonErrorCodes.YOU_CAN_DO_THIS_ACTION_ONLY_WHEN_BID_AT_OPEN_STATE);

            var canCompanyBuyTermsBookValidationResult = await GetCurrentUserCompaniesToBuyTermsBookWithForbiddenReasonsIfFoundAsync(bid.Id, companyId);
            if (!canCompanyBuyTermsBookValidationResult.IsSucceeded)
                return OperationResult<GetCompaniesToBuyTermsBookResponse>.FailFrom(canCompanyBuyTermsBookValidationResult);

            if (canCompanyBuyTermsBookValidationResult.Data.Count == 0 || canCompanyBuyTermsBookValidationResult.Data.Count > 1 || canCompanyBuyTermsBookValidationResult.Data.First().Id != companyId)
                return OperationResult<GetCompaniesToBuyTermsBookResponse>.Fail(HttpErrorCode.InvalidInput, CommonErrorCodes.COMPANY_NOT_FOUND);

            var company = canCompanyBuyTermsBookValidationResult.Data.First();

            var getConvinentErrorIfForbiddenReasonsFoundForCompanyToBuyTermsBookResult = GetConvinentErrorForBuyTermsBokkForbiddenReasons(company);
            if (!getConvinentErrorIfForbiddenReasonsFoundForCompanyToBuyTermsBookResult.IsSucceeded)
                return OperationResult<GetCompaniesToBuyTermsBookResponse>.Fail(getConvinentErrorIfForbiddenReasonsFoundForCompanyToBuyTermsBookResult.HttpErrorCode, getConvinentErrorIfForbiddenReasonsFoundForCompanyToBuyTermsBookResult.Code, getConvinentErrorIfForbiddenReasonsFoundForCompanyToBuyTermsBookResult.ErrorMessage);

            return OperationResult<GetCompaniesToBuyTermsBookResponse>.Success(company);
        }

        private async Task<OperationResult<GetFreelancersToBuyTermsBookResponse>> CanFreelancerBuyTermsBook(Bid bid, long? freelancerId)
        {
            if (bid is null || bid.BidTypeId != (int)BidTypes.Freelancing)
                return OperationResult<GetFreelancersToBuyTermsBookResponse>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.INVALID_BID);

            if (!bid.IsAbleToSubscribeToBid)
                return OperationResult<GetFreelancersToBuyTermsBookResponse>.Fail(HttpErrorCode.InvalidInput, CommonErrorCodes.SUBSCRIPTION_IN_THIS_BID_HAS_BEEN_SUSPENDED);

            if (bid.BidStatusId != (int)TenderStatus.Open)
                return OperationResult<GetFreelancersToBuyTermsBookResponse>.Fail(HttpErrorCode.InvalidInput, CommonErrorCodes.YOU_CAN_DO_THIS_ACTION_ONLY_WHEN_BID_AT_OPEN_STATE);

            var canFreelancerBuyTermsBookValidationResult = await GetCurrentUserFreelancersToBuyTermsBookWithForbiddenReasonsIfFoundAsync(bid.Id, freelancerId);
            if (!canFreelancerBuyTermsBookValidationResult.IsSucceeded)
                return OperationResult<GetFreelancersToBuyTermsBookResponse>.Fail(canFreelancerBuyTermsBookValidationResult.HttpErrorCode, canFreelancerBuyTermsBookValidationResult.Code, canFreelancerBuyTermsBookValidationResult.ErrorMessage);

            if (canFreelancerBuyTermsBookValidationResult.Data !=null && canFreelancerBuyTermsBookValidationResult.Data.Id != freelancerId)
                return OperationResult<GetFreelancersToBuyTermsBookResponse>.Fail(HttpErrorCode.InvalidInput, CommonErrorCodes.FREELANCER_NOT_FOUND);
            

            var getConvinentErrorIfForbiddenReasonsFoundForFreelancerToBuyTermsBookResult = GetConvinentErrorForBuyTermsBookForbiddenReasons(canFreelancerBuyTermsBookValidationResult.Data);
            if (!getConvinentErrorIfForbiddenReasonsFoundForFreelancerToBuyTermsBookResult.IsSucceeded)
                return OperationResult<GetFreelancersToBuyTermsBookResponse>.Fail(getConvinentErrorIfForbiddenReasonsFoundForFreelancerToBuyTermsBookResult.HttpErrorCode, getConvinentErrorIfForbiddenReasonsFoundForFreelancerToBuyTermsBookResult.Code, getConvinentErrorIfForbiddenReasonsFoundForFreelancerToBuyTermsBookResult.ErrorMessage);

            return OperationResult<GetFreelancersToBuyTermsBookResponse>.Success(canFreelancerBuyTermsBookValidationResult.Data);
        }

        //3
        private OperationResult<bool> GetConvinentErrorForBuyTermsBokkForbiddenReasons(GetCompaniesToBuyTermsBookResponse company)
        {
            if (company.IsAbleToBuy)
                return OperationResult<bool>.Success(true);

            if (company.BuyTermsBookForbiddenReasons.Any(x => x == BuyTermsBookForbiddenReasons.CompanyRegisterationExpiryDateIsExpired))
                return OperationResult<bool>.Fail(HttpErrorCode.Conflict, CommonErrorCodes.COMMERCIAL_RECORD_IS_EXPIRED);//

            else if (company.BuyTermsBookForbiddenReasons.Any(x => x == BuyTermsBookForbiddenReasons.CompanyIsNotAssignedByNonProfitEntity))
                return OperationResult<bool>.Fail(HttpErrorCode.Conflict, CommonErrorCodes.COMPANY_NOT_ALLOWED);

            else if (company.BuyTermsBookForbiddenReasons.Any(x => x == BuyTermsBookForbiddenReasons.CompanyNotInvitedInLimitedBid))
                return OperationResult<bool>.Fail(HttpErrorCode.Conflict, CommonErrorCodes.COMPANY_NOT_ALLOWED);

            else if (company.BuyTermsBookForbiddenReasons.Any(x => x == BuyTermsBookForbiddenReasons.UserNotHaveBuyTermsBookPermission))
                return OperationResult<bool>.Fail(HttpErrorCode.Conflict, CommonErrorCodes.COMPANY_NOT_ALLOWED);//

            else if (company.BuyTermsBookForbiddenReasons.Any(x => x == BuyTermsBookForbiddenReasons.CompanyBoughtTermsBookBefore))
                return OperationResult<bool>.Fail(HttpErrorCode.Conflict, BidErrorCodes.YOU_BOUGHT_TERMS_BOOK_BEFORE);//

            else if (company.BuyTermsBookForbiddenReasons.Any(x => x == BuyTermsBookForbiddenReasons.CompanyDelegationFileNotUploaded))
                return OperationResult<bool>.Fail(HttpErrorCode.Conflict, CommonErrorCodes.DELEGATION_FILE_NOT_FOUND);

            else if (company.BuyTermsBookForbiddenReasons.Any(x => x == BuyTermsBookForbiddenReasons.CompanyIsNotSubscribedInSystem))
                return OperationResult<bool>.Fail(HttpErrorCode.Conflict, CommonErrorCodes.ACCOUNT_MUST_BE_SUBSCRIBED);//

            return OperationResult<bool>.Fail(HttpErrorCode.Conflict, CommonErrorCodes.COMPANY_NOT_ALLOWED);
        }
        private OperationResult<bool> GetConvinentErrorForBuyTermsBookForbiddenReasons(GetFreelancersToBuyTermsBookResponse freelancer)
        {
            if (freelancer.IsAbleToBuy)
                return OperationResult<bool>.Success(true);

            if (freelancer.BuyTermsBookForbiddenReasons.Any(x => x == FreelancerBuyTermsBookForbiddenReasons.FreelancerRegisterationExpiryDateIsExpired))
                return OperationResult<bool>.Fail(HttpErrorCode.Conflict, CommonErrorCodes.FREELANCER_DOCUMENT_IS_EXPIRED);


            else if (freelancer.BuyTermsBookForbiddenReasons.Any(x => x == FreelancerBuyTermsBookForbiddenReasons.UserNotHaveBuyTermsBookPermission))
                return OperationResult<bool>.Fail(HttpErrorCode.Conflict, CommonErrorCodes.FREELANCER_NOT_ALLOWED_TO_SUBSCRIBE_TO_BID);

            else if (freelancer.BuyTermsBookForbiddenReasons.Any(x => x == FreelancerBuyTermsBookForbiddenReasons.FreelancerBoughtTermsBookBefore))
                return OperationResult<bool>.Fail(HttpErrorCode.Conflict, BidErrorCodes.YOU_BOUGHT_TERMS_BOOK_BEFORE);

            else if (freelancer.BuyTermsBookForbiddenReasons.Any(x => x == FreelancerBuyTermsBookForbiddenReasons.FreelancerIsNotSubscribedInSystem))
                return OperationResult<bool>.Fail(HttpErrorCode.Conflict, CommonErrorCodes.ACCOUNT_MUST_BE_SUBSCRIBED); 
            
            return OperationResult<bool>.Fail(HttpErrorCode.Conflict, CommonErrorCodes.FREELANCER_NOT_ALLOWED_TO_SUBSCRIBE_TO_BID);
        }

        private async Task<ProviderBid> ApplyCouponIfExist(ProviderBid entity)
        {
            if (!string.IsNullOrEmpty(entity.CouponHash))
            {
                var createdProviderBid =
                    await _providerBidRepository.
                 Find(x => x.Id == entity.Id)
                 .Include(x => x.Company).ThenInclude(x => x.Provider)
                 .Include(x=>x.Freelancer)
                 .Include(x => x.Bid).ThenInclude(x => x.BidAddressesTime)
                 .Include(x => x.Bid).ThenInclude(x => x.Association)
                 .Include(x => x.Bid).ThenInclude(x => x.Donor)
                 .Include(x => x.Bid.Bid_Industries).ThenInclude(x => x.CommercialSectorsTree.Parent)
                 .Include(x => x.Bid.FreelanceBidIndustries).ThenInclude(x => x.FreelanceWorkingSector.Parent).FirstOrDefaultAsync();

                return await _bidAndCouponServicesCommonMethods.SaveCouponDataUsageHistoryForProviderBidAsync(createdProviderBid, entity.CompanyId != null);

            }
            return entity;
        }
        private async Task LogBuyTenderTermsBookEvent(Bid bid, ProviderBid providerBid, string entityName)
        {
            //===============log event===============

            string[] styles = await _helperService.GetEventStyle( EventTypes.BuyTenderTermsBook);
            await _helperService.LogBidEvents(new BidEventModel
            {
                BidId = bid.Id,
                BidStatus = (TenderStatus)bid.BidStatusId,
                BidEventSection = BidEventSections.Bid,
                BidEventTypeId = (int)EventTypes.BuyTenderTermsBook,
                EventCreationDate = _dateTimeZone.CurrentDate,
                ActionId = bid.Id,
                Audience = AudienceTypes.All,
                CompanyId = providerBid.CompanyId ?? 0 ,
                FreelancerId = providerBid.FreelancerId?? 0,
                Header = string.Format(styles[0], fileSettings.ONLINE_URL, providerBid.CompanyId ?? providerBid.FreelancerId, entityName, providerBid.CreationDate.ToString("dddd d MMMM، yyyy , h:mm tt", new CultureInfo("ar-AE")), providerBid.CompanyId is not null ? UserType.Provider : UserType.Freelancer),
                Notes1 = string.Format(styles[1], fileSettings.ONLINE_URL, providerBid.CompanyId ?? providerBid.FreelancerId, entityName, providerBid.CompanyId is not null ? UserType.Provider : UserType.Freelancer)
            });
        }
        private static void MapProviderBidTaxes(AppGeneralSetting generalSettings, bool? HasTaxRecordNumber, ProviderBid entity, Bid bid)
        {
            double AssociationFees = bid.Association_Fees;
            double TanafosFees = bid.Tanafos_Fees;
            if (!string.IsNullOrEmpty(entity.CouponHash) || entity.SubscriptionAddonId.HasValue)
            {
                AssociationFees = entity.AssociationFeesAfterDiscount;
                TanafosFees = entity.TanafosFeesAfterDiscount;
            }

            entity.TaxPercentage = (decimal)generalSettings.VATPercentage;
            entity.TotalAssociationTaxAmount = (decimal)((generalSettings.VATPercentage / 100) * AssociationFees);
            if (HasTaxRecordNumber == true)
            {
                entity.AssociationTaxesAmount = (generalSettings.VATPercentage / 100) * AssociationFees;
                entity.TanafosTaxesAmount = (generalSettings.VATPercentage / 100) * TanafosFees;
            }
            if (HasTaxRecordNumber == false || HasTaxRecordNumber == null)
            {
                entity.AssociationTaxesAmount = 0;
                entity.TanafosTaxesAmount = (AssociationFees + TanafosFees) * (generalSettings.VATPercentage / 100);
            }
        }



        public async Task<OperationResult<bool>> UpdateReadProviderRead(long id)
        {
            try
            {
                var usr = _currentUserService.CurrentUser;
                if (usr == null || usr.UserType != UserType.Association)
                {
                    return OperationResult<bool>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);
                }
                var providerbid = await _providerBidRepository.FindOneAsync(x => x.Id == id);
                providerbid.AssociationRead = true;
                await _providerBidRepository.Update(providerbid);
                return OperationResult<bool>.Success(true);


            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = $"providerBidId = {id}",
                    ErrorMessage = "Failed to Update Read Provider Read!",
                    ControllerAndAction = "BidController/UpdateReadProviderRead"
                });
                return OperationResult<bool>.ServerError(ex, refNo);
            }
        }

        public async Task<PagedResponse<IReadOnlyList<GetProvidersBidsReadOnly>>> GetAssociationProviderBids(int pageSize = 10, int pageNumber = 1)
        {
            try
            {
                var user = _currentUserService.CurrentUser;
                if (user is null)
                    return new PagedResponse<IReadOnlyList<GetProvidersBidsReadOnly>>(HttpErrorCode.NotAuthenticated);
                if (user == null || user.UserType != UserType.Association)
                {
                    return new PagedResponse<IReadOnlyList<GetProvidersBidsReadOnly>>(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);
                }
                List<ProviderBid> providerBids = new List<ProviderBid>();

                var association = await _associationService.GetUserAssociation(user.Email);
                if (association == null)
                    return new PagedResponse<IReadOnlyList<GetProvidersBidsReadOnly>>(HttpErrorCode.NotFound, CommonErrorCodes.ASSOCIATION_NOT_FOUND);

                providerBids = _providerBidRepository.Find(x => x.Bid.AssociationId == association.Id && x.IsPaymentConfirmed && x.Bid.BidAddressesTime.OffersOpeningDate > _dateTimeZone.CurrentDate, false, nameof(ProviderBid.Bid), nameof(ProviderBid.Company)).ToList();

                if (providerBids.Count() <= 0)
                {

                    //return new PagedResponse<IReadOnlyList<GetProvidersBidsReadOnly>>(
                    //       HttpErrorCode.NotFound, CommonErrorCodes.NOT_FOUND);          
                    return new PagedResponse<IReadOnlyList<GetProvidersBidsReadOnly>>(
                           new List<GetProvidersBidsReadOnly>() as IReadOnlyList<GetProvidersBidsReadOnly>, pageNumber, pageSize, 0);
                }
                int totalRecords = providerBids.Count();
                providerBids = providerBids.OrderByDescending(a => a.Bid.CreationDate).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                List<GetProvidersBidsReadOnly> models = new List<GetProvidersBidsReadOnly>();
                foreach (var bid in providerBids)
                {

                    var bidAddressesTime = await _bidAddressesTimeRepository.FindOneAsync(x => x.BidId == bid.BidId);
                    int days = 0;
                    int hour = 0;
                    if (bidAddressesTime.LastDateInOffersSubmission > _dateTimeZone.CurrentDate)
                    {
                        var countdwnon = ((DateTime)bidAddressesTime.LastDateInOffersSubmission).Subtract(_dateTimeZone.CurrentDate).TotalHours;
                        days = (int)(countdwnon / 24);

                        hour = (int)(countdwnon % 24);

                    }
                    //   var association = _bidRepository.FindOneAsync(x => x.Id == bid.BidId, false, nameof(Bid.Association));
                    // var transactionNumber = _payTabTransactionRepository.Find(x => x.CartId == bid.TransactionNumber).FirstOrDefault();

                    var paymentBasicData = new PaymentTransactionBasicData
                    {
                        TransactionNumber = bid.TransactionNumber,
                        PaymentMethod = (Nafes.CrossCutting.Model.Enums.PaymentMethod)bid.PaymentMethodId
                    };
                    var payments = await _helperService.GetPaymentTransaction(new List<PaymentTransactionBasicData> { paymentBasicData });

                    GetProvidersBidsReadOnly model = new GetProvidersBidsReadOnly
                    {
                        AssociationLogoResponse = await _imageService.GetFileResponseEncrypted(association.Image, association.ImageFileName),
                        CompanyLogoResponse = await _imageService.GetFileResponseEncrypted(bid.Company.Image, bid.Company.ImageFileName),
                        AssociationName = association.Association_Name,
                        TransactionNumber = payments == null ? "" : payments.FirstOrDefault().TranRef,
                        CreationDate = bid.CreationDate,
                        Price = bid.Bid.Association_Fees,
                        Id = bid.BidId,
                        Title = bid.Bid.BidName,
                        CountdownToCompleteDays = days,
                        CountdownToCompleteHours = hour
                    };

                    models.Add(model);
                }

                var bidsModels = _mapper.Map<IReadOnlyList<GetProvidersBidsReadOnly>>(models.ToList());
                //bidsModels.ToList().ForEach(x => x.AssociationLogo = !string.IsNullOrEmpty(x.AssociationLogo) ? fileSettings.BASE_URL + x.AssociationLogo : x.AssociationLogo);
                //bidsModels.ToList().ForEach(x => x.CompanyLogo = !string.IsNullOrEmpty(x.CompanyLogo) ? fileSettings.BASE_URL + x.CompanyLogo : x.CompanyLogo);

                return new PagedResponse<IReadOnlyList<GetProvidersBidsReadOnly>>(bidsModels, pageNumber, pageSize, totalRecords);


            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = $"pageNumber = {pageNumber} & pageSize = {pageSize}",
                    ErrorMessage = "Failed to Get Association Provider Bids!",
                    ControllerAndAction = "ProviderController/GetAssociationProviderBids"
                });
                return new PagedResponse<IReadOnlyList<GetProvidersBidsReadOnly>>(HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }

        public async Task<OperationResult<QuantityStableSettings>> QuantityStableSettings()
        {
            var response = new QuantityStableSettings();
            //response.MinimumPercentage = _generalSettings.MinimumValue;
            //response.TanfasRate = _generalSettings.TanfasPercentage;     
            response.MinimumPercentage = (await _appGeneralSettingService.GetAppGeneralSettings()).Data.MinimumValue;
            response.TanfasRate = (await _appGeneralSettingService.GetAppGeneralSettings()).Data.TanfasPercentage;
            return OperationResult<QuantityStableSettings>.Success(response);
        }

        private async Task<List<long>> GetCompanyIdsWhoBoughtTermsPolicy(long BidId)
        {
            var companyIds = await _providerBidRepository.Find(x => x.BidId == BidId && x.IsPaymentConfirmed)
            .Select(a => a.CompanyId??0)
            .ToListAsync();

            return companyIds;
        }
      

        public async Task<(List<NotificationReceiverUser> ActualReceivers, List<NotificationReceiverUser> RealtimeReceivers)> GetProvidersUserIdsWhoBoughtTermsPolicyForNotification(Bid bid)
        {
            if (bid.BidTypeId == (int)BidTypes.Freelancing)
            {
                var freelancersIds = (await _helperService.GetBidTermsBookBuyersDataAsync(bid)).Select(x => x.EntityId);
                var freelancersRecieversUserIds = await _notificationUserClaim.GetUsersClaimOfMultipleIds(new string[] { FreelancerClaimCodes.clm_8003.ToString(), FreelancerClaimCodes.clm_8001.ToString() },
                    freelancersIds.Select(x => (x, OrganizationType.Freelancer)).ToList());
                return freelancersRecieversUserIds;

            }
            var CompanyIds = await GetCompanyIdsWhoBoughtTermsPolicy(bid.Id);
            var recieversUserIds = await _notificationUserClaim.GetUsersClaimOfMultipleIds(new string[] { ProviderClaimCodes.clm_3039.ToString(), ProviderClaimCodes.clm_3041.ToString() },
                CompanyIds.Select(x => (x, OrganizationType.Comapny)).ToList());

            return recieversUserIds;
        }

        public async Task<PagedResponse<IReadOnlyList<ReadOnlyPublicBidListModel>>> GetPublicBidsList(int pageSize, int pageNumber)
        {
            try
            {
                var bidTypes = await _bidTypeRepository
                    .Find(b => !b.IsDeleted && b.IsVisible)
                    .Where(x => x.Id != (int)BidTypes.Freelancing)
                    .Select(x => x.Id)
                    .ToListAsync();

                var bids = _bidRepository
                    .Find(x =>
                    bidTypes.Contains((int)(x.BidTypeId))
                    && x.BidStatusId != (int)TenderStatus.Draft
                    && x.BidStatusId != (int)TenderStatus.Cancelled
                    && x.BidStatusId != (int)TenderStatus.Reviewing
                    && !x.IsBidAssignedForAssociationsOnly
                    && !x.IsBidHidden, false);

                return await HandlePublicBidsQuery(pageSize, pageNumber, bids);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = $"pageNumber = {pageNumber} & pageSize = {pageSize}",
                    ErrorMessage = "Failed to Get public bids list!",
                    ControllerAndAction = "BidController/public-bids-list"
                });
                return new PagedResponse<IReadOnlyList<ReadOnlyPublicBidListModel>>(
                       HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }

        public async Task<PagedResponse<IReadOnlyList<ReadOnlyPublicBidListModel>>> GetPublicFreelancingBidsList(FilterBidsSearchModel request)
        {
            try
            {
                var user = _currentUserService.CurrentUser;
                if (user is not null && user.UserType == UserType.Provider)
                    return new PagedResponse<IReadOnlyList<ReadOnlyPublicBidListModel>>(HttpErrorCode.NotAuthorized, CommonErrorCodes.NotAuthorized);

                var bids = _bidRepository.Find(x => x.BidTypeId == (int)BidTypes.Freelancing
                    && x.BidStatusId != (int)TenderStatus.Draft
                    && x.BidStatusId != (int)TenderStatus.Cancelled
                    && x.BidStatusId != (int)TenderStatus.Reviewing
                    && !x.IsBidAssignedForAssociationsOnly
                    && !x.IsBidHidden, false);
                bids= await ApplyFiltrationForBids(request, bids,getFreelancingBids:true);
                return await HandlePublicBidsQuery(request.pageSize,request.pageNumber, bids);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = request,
                    ErrorMessage = "Failed to Get Public Freelancing Bids List!",
                    ControllerAndAction = "BidController/freelancing-public-bids-list"
                });
                return new PagedResponse<IReadOnlyList<ReadOnlyPublicBidListModel>>(HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }


        private async Task<PagedResponse<IReadOnlyList<ReadOnlyPublicBidListModel>>> HandlePublicBidsQuery(int pageSize, int pageNumber, IQueryable<Bid> bids)
        {
            int totalRecords = await bids.CountAsync();
            if (bids == null || totalRecords == 0)
                return new PagedResponse<IReadOnlyList<ReadOnlyPublicBidListModel>>(null, pageNumber, pageSize);

            List<Bid> bidsList = await GetBidsList(pageSize, pageNumber, bids);

            var bidsModels = new List<ReadOnlyPublicBidListModel>();
            await MapBidsModels(bidsList, bidsModels);

            var user = _currentUserService?.CurrentUser;
            if (user != null)
                await MapCurrentUserData(bidsModels, user);
            else
                bidsModels.ForEach(x => { x.Price = null; x.IsUserFavorite = null; x.hasProviderMatchIndustries = null; });

            return new PagedResponse<IReadOnlyList<ReadOnlyPublicBidListModel>>(bidsModels, pageNumber, pageSize, totalRecords);
        }
        private async Task MapCurrentUserData(List<ReadOnlyPublicBidListModel> bidsModels, ApplicationUser user)
        {
            var userFavBids = (await _userFavBidList.Find(x => x.UserId == user.Id).ToListAsync()).ToDictionary(x => x.BidId);
            var bidsIds = bidsModels.Select(x => x.Id);

            Company company = null;
            Freelancer freelancer = null;
            List<IndustryMiniModel> bidParticipantWorkingSectorsMiniData = new();

            if (user.UserType == UserType.Provider)
            {
                company = await _companyRepository.Find(x => x.IsDeleted == false && x.isVerfied == true && x.Id == user.CurrentOrgnizationId, false, nameof(Company.Provider))
                    .Include(x => x.Company_Industries)
                        .ThenInclude(x => x.CommercialSectorsTree)
                    .FirstOrDefaultAsync();

                if (company is not null)
                    bidParticipantWorkingSectorsMiniData = company.Company_Industries.Where(x => x.CommercialSectorsTreeId.HasValue).Select(x => new IndustryMiniModel { Id = x.CommercialSectorsTreeId.Value, ParentId = x.CommercialSectorsTree.ParentId }).ToList();
            }
            else if (user.UserType == UserType.Freelancer)
            {
                freelancer = await _freelancerRepository.Find(x => x.IsVerified && x.Id == user.CurrentOrgnizationId)
                    .Include(x => x.FreelancerWorkingSectors)
                        .ThenInclude(x => x.FreelanceWorkingSector)
                    .FirstOrDefaultAsync();

                if (freelancer is not null)
                    bidParticipantWorkingSectorsMiniData = freelancer.FreelancerWorkingSectors.Select(x => new IndustryMiniModel { Id = x.FreelanceWorkingSectorId, ParentId = x.FreelanceWorkingSector.ParentId }).ToList();
            }


            foreach (var itm in bidsModels)
            {
                if (user.UserType == UserType.Provider || user.UserType == UserType.Freelancer)
                    itm.hasProviderMatchIndustries = itm.BidMainClassificationId.Any(x => bidParticipantWorkingSectorsMiniData.Any(y => y.ParentId == x.ParentId));

                if (userFavBids.ContainsKey(itm.Id))
                    itm.IsUserFavorite = true;
                else
                    itm.IsUserFavorite = false;
            }            
        }

        private async Task MapBidsModels(List<Bid> bidsList, List<ReadOnlyPublicBidListModel> bidsModels)
        {
            int regionCount = await _regionRepository.Find(a => true, true, false).CountAsync();
            foreach (var bid in bidsList)
            {
                var model = new ReadOnlyPublicBidListModel();
                await MapEntityData(bid, model);

                model.Title = bid.BidName;
                model.Ref_Number = bid.Ref_Number;
                model.BidOffersSubmissionTypeId = bid.BidOffersSubmissionTypeId;
                model.BidStatusId = bid.BidStatusId.Value;
                model.BidTypeId = bid.BidTypeId;
                model.BidTypeName = bid.BidType.NameAr;
                model.Price = bid.Bid_Documents_Price;
                model.Id = bid.Id;
                model.LastDateInOffersSubmission = bid.BidAddressesTime?.LastDateInOffersSubmission?.ToString("yyyy-MM-ddTHH:mm:ss");
                model.Regions = bid.BidRegions.Select(x => x.RegionId).ToList();// BidRegion.getAllRegionsAsListOfIds(bid.BidRegions);
                model.RegionsNames = bid.BidRegions.Select(b => b.Region.NameAr).ToList();
                model.IsUserFavorite = false;

                if (model.RegionsNames.Count == regionCount)
                {
                    model.RegionsNames.Clear();
                    model.RegionsNames.Add(Constants.AllRegionsArabic);
                }

                var bidWorkingSectors = bid.GetBidWorkingSectors();

                model.BidMainClassificationId = bidWorkingSectors.Select(a => new BidMainClassificationIds { Id = a.Id, ParentId = a.ParentId }).ToList();
                model.BidMainClassificationNames = bidWorkingSectors.Select(i => new BidMainClassificationNames { Name = i.NameAr, ParentName = i?.Parent?.NameAr }).ToList();

                bidsModels.Add(model);
            }
        }

        private static async Task<List<Bid>> GetBidsList(int pageSize, int pageNumber, IQueryable<Bid> bids)
        {
            return await bids
                .OrderByDescending(c => c.CreationDate)
                .ApplyPaging(pageNumber, pageSize)
                .IncludeBasicBidData()
                .Include(a => a.BidMainClassificationMapping)
                .Include(a => a.BidStatus)
                .Include(a => a.BidType)
                .Include(a => a.BidRegions)
                .ThenInclude(b => b.Region)
                .Include(a => a.BidDonor)
                .Include(a => a.BidDonor.Donor)
                .AsSplitQuery()
                .ToListAsync();
        }

        private async Task MapEntityData(Bid bid, ReadOnlyPublicBidListModel model)
        {
            //var user = _currentUserService?.CurrentUser;
            //if (user != null)
            //{
            if (bid.EntityType == UserType.Association)
            {
                model.EntityImage = await _imageService.GetFileResponseEncrypted(bid.Association.Image, bid.Association.ImageFileName);
                model.EntityName = bid.Association.Association_Name;
                model.EntityType = bid.EntityType;
                model.EntityId = bid.EntityId;
            }
            else if (bid.EntityType == UserType.Donor)
            {

                model.EntityImage = await _imageService.GetFileResponseEncrypted(bid.Donor.Image, bid.Donor.ImageFileName);
                model.EntityName = bid.Donor.DonorName;
                model.EntityType = bid.EntityType;
                model.EntityId = bid.EntityId;
                //}
            }
        }

        public async Task<OperationResult<ReadOnlyPublicBidModel>> GetPublicBidDetails(long id)
        {
            try
            {
                int?[] bidStatus = { 1, 10, 11, 12, 13 };

                var bid = await _bidRepository.Find(x => x.Id == id && bidStatus.Contains(x.BidStatusId)) //*&& x.BidVisibility == BidTypes.Public*///, false, nameof(Bid.BidAddressesTime),
                   .IncludeBasicBidData()
                   .Include(a => a.BidStatus)
                   .Include(a => a.BidOffersSubmissionType)
                   .Include(a => a.BidRegions).ThenInclude(x => x.Region)
                   .Include(a => a.BidDonor).ThenInclude(a => a.Donor)
                   .FirstOrDefaultAsync();

                if (bid == null)
                    return OperationResult<ReadOnlyPublicBidModel>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.NOT_FOUND);

                var model = _mapper.Map<ReadOnlyPublicBidModel>(bid);
                if (bid.EntityType == UserType.Association)
                {
                    model.Entity_Image = await _imageService.GetFileResponseEncrypted(bid.Association.Image, bid.Association.ImageFileName);
                    model.Entity_Name = bid.Association.Association_Name;
                    model.EntityType = bid.EntityType;
                    model.EntityId = bid.EntityId;
                }
                else if (bid.EntityType == UserType.Donor)
                {
                    model.Entity_Image = await _imageService.GetFileResponseEncrypted(bid.Donor.Image, bid.Donor.ImageFileName);
                    model.Entity_Name = bid.Donor.DonorName;
                    model.EntityType = bid.EntityType;
                    model.EntityId = bid.EntityId;
                }

                model.BidDonorName = bid.BidDonorId.HasValue ? bid.BidDonor.DonorId.HasValue ? bid.BidDonor.Donor.DonorName : bid.BidDonor.NewDonorName : "";
                model.BidOffersSubmissionTypeName = bid.BidOffersSubmissionType?.NameAr;

                model.Regions = bid.BidRegions?.Select(a => a.Region?.NameAr).ToList();

                var bidWorkingSectors = bid.GetBidWorkingSectors().Select(a => a.NameAr);
                model.IndustriesName = string.Join(" ، ", bidWorkingSectors);

                return OperationResult<ReadOnlyPublicBidModel>.Success(model);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = $"Bid Id  = {id}",
                    ErrorMessage = "Failed to Get public bids Details!",
                    ControllerAndAction = "BidController/public-bid-details/{id}"
                });
                return OperationResult<ReadOnlyPublicBidModel>.Fail(
                       HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }
        //public async Task<OperationResult<string>> GetZipFileForBidAttachment(long bidId)
        //{
        //    try
        //    {

        //    var bidAttachmentUrls = await _bidAttachmentRepository.Find(x => x.BidId == bidId, false).
        //            Select(x => x.AttachedFileURL).ToListAsync();
        //    var bidName = (await _bidRepository.GetById(bidId)).BidName;

        //    if (bidAttachmentUrls == null || bidAttachmentUrls.Count==0)
        //        return OperationResult<string>.Success(null);

        //    var zipFilePath = await _compressService.GetZipFile(bidAttachmentUrls, bidName);
        //        var url=  fileSettings.BASE_URL + zipFilePath ;
        //    return  OperationResult<string>.Success(url); ;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogCritical(ex, $"Failed to compress attachnets!");
        //        return OperationResult<string>.Fail(
        //               HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED);
        //    }
        //}

        public async Task<(OperationResult<byte[]>, string)> GetZipFileForBidAttachmentAsBinary(long bidId)
        {
            try
            {

                var bidAttachmentUrls = await _bidAttachmentRepository.Find(x => x.BidId == bidId, false).
                        Select(x => x.AttachedFileURL).ToListAsync();
                var bidName = (await _bidRepository.GetById(bidId)).BidName;

                if (bidAttachmentUrls == null || bidAttachmentUrls.Count == 0)
                    return (OperationResult<byte[]>.Success(null), null);

                var zipfileBinary = await _compressService.GetZipFileAsbinary(bidAttachmentUrls);


                return (OperationResult<byte[]>.Success(zipfileBinary), $"{bidName}_{_randomGeneratorService.RandomNumber(1000, 9999)}"); ;
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = $"Bid Id = {bidId}",
                    ErrorMessage = "Failed to Get Zip File For Bid Attachment As Binary!",
                    ControllerAndAction = ""
                });
                return (OperationResult<byte[]>.Fail(
                       HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED), refNo);
            }
        }


        public async Task<OperationResult<bool>> IsBidInEvaluation(long bidId)
        {
            try
            {
                var listOfAllowedUserTypes = new List<UserType>
                    { UserType.SuperAdmin,UserType.Admin, UserType.SupportManager, UserType.SupportMember, UserType.Association };
                var usr = _currentUserService.CurrentUser;
                if (usr == null || !listOfAllowedUserTypes.Contains(usr.UserType))
                {
                    return OperationResult<bool>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);
                }

                var bid = await _bidRepository.FindOneAsync(x => x.Id == bidId, false, nameof(Bid.BidAddressesTime));
                if (bid == null)
                    return OperationResult<bool>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.NOT_FOUND);

                var check = DateTime.Compare(_dateTimeZone.CurrentDate, (DateTime)bid.BidAddressesTime.OffersOpeningDate);
                // check < 0 show count
                // check >= 0 data
                return OperationResult<bool>.Success(check >= 0 ? true : false);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = $"Bid Id = {bidId}",
                    ErrorMessage = "Failed to check if bid is in evaluation stage!",
                    ControllerAndAction = "BidController/is-bid-in-evaluation-stage"
                });
                return OperationResult<bool>.Fail(HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }

        public async Task<OperationResult<int>> GetStoppingPeriod()
        {
            try
            {
                var listOfAllowedUserTypes = new List<UserType>
                    { UserType.SuperAdmin,UserType.Admin, UserType.SupportManager, UserType.SupportMember, UserType.Association, UserType.Provider, UserType.Company,UserType.Donor };
                var usr = _currentUserService.CurrentUser;
                if (usr == null || !listOfAllowedUserTypes.Contains(usr.UserType))
                    return OperationResult<int>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);

                var generalSettingsResult = await _appGeneralSettingService.GetAppGeneralSettings();
                if (!generalSettingsResult.IsSucceeded)
                    return OperationResult<int>.Fail(generalSettingsResult.HttpErrorCode, generalSettingsResult.Code);

                var generalSettings = generalSettingsResult.Data;

                return OperationResult<int>.Success(generalSettings.StoppingPeriodDays);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = null,
                    ErrorMessage = "Failed to get stopping period for bids!",
                    ControllerAndAction = "BidController/stopping-period"
                });
                return OperationResult<int>.Fail(HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }

        public async Task<OperationResult<GetBidDetailsForShare>> GetBidDetailsForShare(long bidId)
        {
            try
            {
                var bid = await _bidRepository.FindOneAsync(x => x.Id == bidId && !x.IsDeleted, false,
                    nameof(Bid.BidStatus),
                    nameof(Bid.BidAddressesTime)
                    );

                if (bid == null)
                    return OperationResult<GetBidDetailsForShare>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);

                var model = new GetBidDetailsForShare()
                {
                    BidName = bid.BidName,
                    Ref_Number = bid.Ref_Number,
                    BidStatus = bid.BidStatus?.NameAr,
                    LastDateInOffersSubmission = bid.BidAddressesTime != null ? (DateTime)bid.BidAddressesTime.LastDateInOffersSubmission : null
                };
                return OperationResult<GetBidDetailsForShare>.Success(model);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = $"Bid Id = {bidId}",
                    ErrorMessage = "Failed to get bid details for shar!",
                    ControllerAndAction = "BidController/BidDetailsForShare/{id}"
                });
                return OperationResult<GetBidDetailsForShare>.Fail(HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }

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
                    var entityName = await GetBidCreatorName(bid);

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
                            Subject = $"تمديد فترة المنافسة {bid.BidName}",
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
                        Subject = $"تمديد فترة المنافسة {bid.BidName}",
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
                                Message = $"تم تمديد فترة تقديم العروض ل {bid.BidName} لتنتهي بتاريخ {model.LastDateInOffersSubmission}	",
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
                        Header = string.Format(styles[0], fileSettings.ONLINE_URL, bid.EntityType == UserType.Association ? "association" : "donor", bid.EntityId, entityName, _dateTimeZone.CurrentDate.ToString("dddd d MMMM، yyyy , h:mm tt", new CultureInfo("ar-AE"))),
                        Notes1 = string.Format(styles[1], model.LastDateInOffersSubmission.ToString("d MMMM، yyyy", new CultureInfo("ar-AE"))),
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

        public async Task<OperationResult<List<BidStatusResponse>>> GetBidStatusWithDates(long bidId)
        {
            try
            {
                var bidInDb = await _bidRepository.FindOneAsync(x => x.Id == bidId && !x.IsDeleted, false, nameof(Bid.BidAddressesTime));
                if (bidInDb is null)
                    return OperationResult<List<BidStatusResponse>>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);

                if (bidInDb.BidTypeId == (int)BidTypes.Instant 
                    || bidInDb.BidTypeId == (int)BidTypes.Freelancing)
                    return await GetBidStatusWithDatesForInstantBids(bidInDb);

                return await GetBidStatusWithDatesForBid(bidInDb);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = $"bid Id = {bidId}",
                    ErrorMessage = "Failed to get bid status with dates!",
                    ControllerAndAction = "BidController/bid-status/{id}"
                });
                return OperationResult<List<BidStatusResponse>>.Fail(HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }

        private async Task<OperationResult<List<BidStatusResponse>>> GetBidStatusWithDatesNew(Bid bidInDb)
        {
            if (bidInDb is null)
                return OperationResult<List<BidStatusResponse>>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);

            if (bidInDb.BidTypeId == (int)BidTypes.Instant || bidInDb.BidTypeId == (int)BidTypes.Freelancing)
                return await GetBidStatusWithDatesForInstantBids(bidInDb);

            return await GetBidStatusWithDatesForBid(bidInDb);
        }
        public async Task<OperationResult<List<BidStatusResponse>>> GetBidStatusWithDatesForBid(Bid bidInDb)
        {
            if (bidInDb.BidStatusId == (int)TenderStatus.Draft)
                return OperationResult<List<BidStatusResponse>>.Success(new List<BidStatusResponse>());

            if (bidInDb.BidAddressesTime == null)
                return OperationResult<List<BidStatusResponse>>.Fail(HttpErrorCode.NotFound, BidErrorCodes.BID_ADDRESSES_TIMES_HAS_NO_DATA);

            var statusesInDb = await _bidStatusRepository.FindAsync(s => !s.IsDeleted);

            var model = new List<BidStatusResponse>();
            model.AddRange(statusesInDb.Where(s => s.Id != (int)TenderStatus.Draft).Select(s => new BidStatusResponse
            {
                BidStatus = (TenderStatus)s.Id,
                Name = s.NameAr
            }));

            // exclude cancelled object it not cancelled
            if (bidInDb.BidStatusId != (int)TenderStatus.Cancelled)
                model = model.Where(s => s.BidStatus != TenderStatus.Cancelled).ToList();
            // exclude Stopping object if StoppingPeriod is zero
            if (bidInDb.BidAddressesTime.StoppingPeriod == 0)
                model.RemoveAll(s => s.BidStatus == TenderStatus.Stopping);

            var generalSettingsResult = await _appGeneralSettingService.GetAppGeneralSettings();
            if (!generalSettingsResult.IsSucceeded)
                return OperationResult<List<BidStatusResponse>>.Fail(generalSettingsResult.HttpErrorCode, generalSettingsResult.Code);

            var generalSettings = generalSettingsResult.Data;

            foreach (var item in model)
            {
                switch (item.BidStatus)
                {
                    case TenderStatus.Reviewing:
                        // from creation to LastDateInOffersSubmission
                        item.From = bidInDb.CreationDate;
                        item.To = null;
                        item.Index = 1;
                        item.IsCurrentPhase = bidInDb.BidStatusId == (int)TenderStatus.Reviewing;
                        break;

                    case TenderStatus.Open:
                        // from creation to LastDateInOffersSubmission
                        item.From = bidInDb.CreationDate;
                        item.To = bidInDb.BidAddressesTime.LastDateInOffersSubmission;
                        item.IsCurrentPhase = bidInDb.BidStatusId == (int)TenderStatus.Open;
                        item.Index = 2;
                        break;

                    case TenderStatus.Evaluation:
                        // from OffersOpeningDate to confirmation date
                        item.From = bidInDb.BidAddressesTime.OffersOpeningDate;
                        item.To = bidInDb.BidAddressesTime.ConfirmationDate;
                        item.IsCurrentPhase = bidInDb.BidStatusId == (int)TenderStatus.Evaluation;
                        item.Index = 3;
                        break;

                    case TenderStatus.Stopping:
                        // from confirmation + 1 to confirmation + StoppingPeriod
                        item.From = bidInDb.BidAddressesTime.ConfirmationDate.HasValue
                        ? bidInDb.BidAddressesTime.ConfirmationDate.Value.AddDays(1)
                        : ((DateTime)bidInDb.BidAddressesTime.OffersOpeningDate).AddDays(1);

                        item.To = bidInDb.BidAddressesTime.ConfirmationDate.HasValue
                            ? bidInDb.BidAddressesTime.ConfirmationDate.Value.AddDays(bidInDb.BidAddressesTime.StoppingPeriod)
                            : ((DateTime)bidInDb.BidAddressesTime.OffersOpeningDate).AddDays(bidInDb.BidAddressesTime.StoppingPeriod);

                        item.IsCurrentPhase = bidInDb.BidStatusId == (int)TenderStatus.Stopping;
                        item.Index = 4;
                        break;

                    case TenderStatus.Awarding:
                        // from ExpectedAwardingDate
                        item.From = bidInDb.BidAddressesTime.ExpectedAnchoringDate;
                        item.IsCurrentPhase = bidInDb.BidStatusId == (int)TenderStatus.Awarding;
                        item.Index = 5;
                        break;

                    case TenderStatus.Cancelled:
                        // modificationDate
                        item.From = bidInDb.ModificationDate;
                        item.IsCurrentPhase = bidInDb.BidStatusId == (int)TenderStatus.Cancelled;
                        item.Index = 7;
                        break;

                    case TenderStatus.Closed:
                        // from ExpectedAwardingDate
                        item.From = bidInDb.ActualAnchoringDate;
                        item.IsCurrentPhase = bidInDb.BidStatusId == (int)TenderStatus.Closed;
                        item.Index = 6;
                        break;

                    default:
                        item.BidStatus = 0;
                        item.Name = null;
                        item.From = null;
                        item.To = null;
                        item.IsCurrentPhase = false;
                        break;
                }
            }
            // order by from date asc
            model = model.OrderBy(m => m.From).OrderBy(x => x.Index).ToList();
            // shift nulls to the end of the list
            foreach (var m in model.Where(m => m.From is null).ToList())
                MoveItemToEnd(m, model);

            var currentItem = model.FirstOrDefault(e => e.IsCurrentPhase);
            if (currentItem != null)
            {
                var index = model.IndexOf(currentItem);
                if (index > 0)
                    foreach (var item in model)
                    {
                        var indexItem = model.IndexOf(item);

                        if (indexItem < index)
                            item.IsDone = true;
                    }
            }

            var ignoreTimeline = await _demoSettingsService.GetIgnoreTimeLineAsync();
            if (ignoreTimeline)
                model = model.ToList().OrderBy(a => a.Index).ToList();
            return OperationResult<List<BidStatusResponse>>.Success(model);
        }

        public async Task<OperationResult<List<BidStatusResponse>>> GetBidStatusWithDatesForInstantBids(Bid bidInDb)
        {
            var instantBidStatuses = await _bidStatusRepository.FindAsync(s => !s.IsDeleted && s.Id == (int)TenderStatus.Open || s.Id == (int)TenderStatus.Closed || s.Id == (int)TenderStatus.Cancelled);

            List<BidStatusResponse> model = CreateAndFillTheResponseModelForStatuses(bidInDb, instantBidStatuses);

            SetIsDoneToTrueForPreviousPhases(model.OrderBy(a => a.Index).ToList());

            return OperationResult<List<BidStatusResponse>>.Success(await OrderTimelineByIndexIfIgnoreTimelineIsTrue(model));
        }

        private static List<BidStatusResponse> CreateAndFillTheResponseModelForStatuses(Bid bidInDb, IEnumerable<Nafes.CrossCutting.Model.Lookups.BidStatus> instantBidStatuses)
        {
            var model = new List<BidStatusResponse>();
            model.AddRange(instantBidStatuses.Select(s => new BidStatusResponse
            {
                BidStatus = (TenderStatus)s.Id,
                Name = s.NameAr
            }));

            // exclude cancelled object it not cancelled
            if (bidInDb.BidStatusId != (int)TenderStatus.Cancelled)
                model = model
                    .Where(s => s.BidStatus != TenderStatus.Cancelled)
                    .ToList();

            foreach (var item in model)
            {
                switch (item.BidStatus)
                {
                    case TenderStatus.Reviewing:
                        // from creation to LastDateInOffersSubmission
                        item.From = bidInDb.CreationDate;
                        item.To = null;
                        item.Index = 1;
                        item.IsCurrentPhase = bidInDb.BidStatusId == (int)TenderStatus.Reviewing;
                        break;

                    case TenderStatus.Open:
                        item.From = null;
                        item.To = null;
                        item.IsCurrentPhase = bidInDb.BidStatusId == (int)TenderStatus.Open || bidInDb.BidStatusId == (int)TenderStatus.Awarding;
                        item.Index = 2;
                        break;

                    case TenderStatus.Cancelled:
                        item.From = null;
                        item.To = null;
                        item.IsCurrentPhase = bidInDb.BidStatusId == (int)TenderStatus.Cancelled;
                        item.Index = 3;
                        break;

                    case TenderStatus.Closed:
                        item.From = null;
                        item.To = null;
                        item.IsCurrentPhase = bidInDb.BidStatusId == (int)TenderStatus.Closed;
                        item.Index = 4;
                        break;

                    default:
                        item.BidStatus = 0;
                        item.Name = null;
                        item.From = null;
                        item.To = null;
                        item.IsCurrentPhase = false;
                        break;
                }
            }

            return model;
        }

        private static void SetIsDoneToTrueForPreviousPhases(List<BidStatusResponse> model)
        {
            var currentPhaseItem = model.FirstOrDefault(e => e.IsCurrentPhase);
            if (currentPhaseItem != null)
            {
                var indexOfCurrentPhaseItem = model.IndexOf(currentPhaseItem);
                foreach (var item in model)
                {
                    var indexItem = model.IndexOf(item);

                    if (indexItem < indexOfCurrentPhaseItem)
                        item.IsDone = true;
                }
            }
        }

        public async Task<List<BidStatusResponse>> OrderTimelineByIndexIfIgnoreTimelineIsTrue(List<BidStatusResponse> model)
        {
            var ignoreTimeline = await _demoSettingsService.GetIgnoreTimeLineAsync();

            if (ignoreTimeline)
                model = model.OrderBy(a => a.Index).ToList();
            return model;
        }

        private void MoveItemToEnd<T>(T item, List<T> list)
        {
            var index = list.IndexOf(item);
            list.RemoveAt(index);
            list.Add(item);
        }

        private async Task<Bid> GetBidWithRelatedEntitiesByIdAsync(long bidId)
        {
            return await _bidRepository
                    .Find(x => x.Id == bidId, true, false)
                    .IncludeBasicBidData()
                    .Include(b => b.BidStatus)
                    .Include(b => b.BidType)
                    .Include(b => b.BidOffersSubmissionType)
                    .Include(b => b.InvitationRequiredDocuments)
                    .Include(b => b.BidCancelationReason)
                    .Include(b => b.BidRegions)
                        .ThenInclude(a => a.Region)
                    .Include(a => a.BidDonor)
                    .Include(a => a.BidSupervisingData)
                        .ThenInclude(a => a.AwardingDataForBidSupervisingRequest)
                        .AsSplitQuery()
                    .FirstOrDefaultAsync();
        }
        public async Task<OperationResult<ReadOnlyBidResponse>> GetDetailsForBidByIdAsync(long bidId)
        {
            try
            {
                var user = _currentUserService.CurrentUser;
                if (user == null)
                    return await MapPublicData(bidId);

                var bid = await GetBidWithRelatedEntitiesByIdAsync(bidId);
                if (bid is null)
                    return OperationResult<ReadOnlyBidResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);

                var model = _mapper.Map<ReadOnlyBidResponse>(bid);
                ReturnDistinctSupervisingDataBasedOnClaimType(model);

                model.InvitedAssociationByDonor = await GetInvitedAssociationIfFound(bid);
                model.Regions = BidRegion.getAllRegionsAsListOfIds(bid.BidRegions);
                model.RegionsNames = bid.BidRegions.Select(b => b.Region.NameAr).ToList();
                model.BidSectorsProviders = bid.BidTypeId == (int)BidTypes.Freelancing ?
                    (await GetFreelancersWithSameWorkingSectors(_freelancerRepository, bid)).Count:
                    (await _bidsOfProviderRepository.GetProvidersEmailsOfCompaniesSubscribedToBidIndustries(bid)).Count;

                var sponsorBidDonor = await _donorService.GetBidDonorOfBidIfFound(bid.Id);
                model.SponsorDonorId = sponsorBidDonor is null ? null : sponsorBidDonor.DonorId;

                //=============== check is bid supervised by donor=============
                if (user.UserType == UserType.Donor)
                {
                    Donor donor = await GetDonorUser(user);
                    if (donor is null)
                        return OperationResult<ReadOnlyBidResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.DONOR_NOT_FOUND);

                    model.IsCurrentEntity = (bid.EntityId == donor.Id && bid.EntityType == UserType.Donor);
                    BidDonor bidDonor = await _BidDonorRepository.Find(a => a.BidId == bidId
                                        && a.DonorId == donor.Id && a.DonorResponse != DonorResponse.Reject) // عشان لو لسه معملش accept
                        .OrderByDescending(a => a.CreationDate)
                        .FirstOrDefaultAsync();

                    if ((!model.IsCurrentEntity && (bid.BidStatusId == (int)TenderStatus.Draft
                      || bid.BidStatusId == (int)TenderStatus.Pending
                      || bid.BidStatusId == (int)TenderStatus.Reviewing))
                      && (bidDonor is null && (bid.BidStatusId == (int)TenderStatus.Draft
                      || bid.BidStatusId == (int)TenderStatus.Pending
                      || bid.BidStatusId == (int)TenderStatus.Reviewing)))
                        return OperationResult<ReadOnlyBidResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);


                    model.donorResponse = bidDonor?.DonorResponse;
                    model.bidDonnerId = bidDonor?.Id;
                }
                if (user.UserType == UserType.Association)
                {
                    var association = await _associationService.GetUserAssociation(user.Email);
                    if (association is null)
                        return OperationResult<ReadOnlyBidResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.ASSOCIATION_NOT_FOUND);

                    model.IsCurrentEntity = bid.AssociationId == association.Id;

                    if ((!model.IsCurrentEntity && (bid.BidStatusId == (int)TenderStatus.Draft
                     || bid.BidStatusId == (int)TenderStatus.Pending
                     || bid.BidStatusId == (int)TenderStatus.Reviewing))
                     && (bid.SupervisingAssociationId != association.Id && (bid.BidStatusId == (int)TenderStatus.Draft
                     || bid.BidStatusId == (int)TenderStatus.Pending
                     || bid.BidStatusId == (int)TenderStatus.Reviewing)))
                        return OperationResult<ReadOnlyBidResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);

                    model.IsSupervisingAssociation = !bid.IsAssociationFoundToSupervise ? false : (bid.SupervisingAssociationId == user.CurrentOrgnizationId);
                }

                Company company = null;
                Freelancer freelancer = null;

                if (user.UserType == UserType.Provider)
                {
                    if (bid.BidStatusId == (int)TenderStatus.Draft
                      || bid.BidStatusId == (int)TenderStatus.Pending
                      || bid.BidStatusId == (int)TenderStatus.Reviewing
                      || (BidTypes)bid.BidTypeId == BidTypes.Freelancing)
                        return OperationResult<ReadOnlyBidResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);

                    company = await _companyRepository.FindOneAsync(x => x.Id == user.CurrentOrgnizationId, false);
                    if (company is null)
                        company = await _companyService.GetUserCompany(user.Email);

                    if (company is null)
                        return OperationResult<ReadOnlyBidResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.COMPANY_NOT_FOUND);

                    if (await CheckIfBidForAssignedComapniesOnly(bid, company))
                        return OperationResult<ReadOnlyBidResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);

                    if (bid.BidTypeId == (int)BidTypes.Private && !await _companyService.IsCompanyInvitedToThisClosedBid(bidId, company.Id, null, company.Commercial_record, company.UniqueNumber700))
                        model.IsInvitedUser = false;
                    else
                        model.IsInvitedUser = true;
                    await MapRevealsData(user, bid, model);
                    var bidCompanyQuotation = await _tenderSubmitQuotationRepository
                            .Find(x => x.BidId == bid.Id && x.ProposalStatus == ProposalStatus.Delivered
                                    && x.CompanyId == company.Id, false)
                            .FirstOrDefaultAsync();
                    if (bidCompanyQuotation is not null)
                    {
                        model.QuotationConfirmationDate = bid.BidAddressesTime?.ConfirmationDate;
                        model.ApplyingQuotationDate = bidCompanyQuotation.CreationDate;
                    }
                }
                if (user.UserType == UserType.Freelancer)
                {
                    if (bid.BidStatusId == (int)TenderStatus.Draft
                      || bid.BidStatusId == (int)TenderStatus.Pending
                      || bid.BidStatusId == (int)TenderStatus.Reviewing
                      || (BidTypes)bid.BidTypeId != BidTypes.Freelancing)
                        return OperationResult<ReadOnlyBidResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);

                    freelancer = await _freelancerRepository.FindOneAsync(x => x.Id == user.CurrentOrgnizationId, false);
                    if (freelancer is null)
                        return OperationResult<ReadOnlyBidResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.FREELANCER_NOT_FOUND);

                    await MapRevealsData(user, bid, model);

                    var bidFreelancerQuotation = await _tenderSubmitQuotationRepository
                            .Find(x => x.BidId == bid.Id && x.ProposalStatus == ProposalStatus.Delivered
                                    && x.FreelancerId == freelancer.Id, false)
                            .FirstOrDefaultAsync();
                    if (bidFreelancerQuotation is not null)
                    {
                        model.QuotationConfirmationDate = bid.BidAddressesTime?.ConfirmationDate;
                        model.ApplyingQuotationDate = bidFreelancerQuotation.CreationDate;
                    }
                }

                if (bid.EntityType == UserType.Association)
                {
                    model.Entity_Image = await _imageService.GetFileResponseEncrypted(bid.Association.Image, bid.Association.ImageFileName);
                    model.Entity_Name = bid.Association.Association_Name;
                    model.EntityId = bid.EntityId;
                    model.EntityType = bid.EntityType;
                    await MapBidCreatorDetailsIfAssociation(user, bid, model);

                    //============check supervising=========================
                    if (bid.SupervisingAssociationId.HasValue)
                    {
                        if (bid.SupervisingAssociationId.Value == -1)
                        {
                            InvitedAssociationsByDonor invitedAss = await _invitedAssociationsByDonorRepository.FindOneAsync(a => a.BidId == bidId);
                            model.SupervisorName = invitedAss is null ? "" : invitedAss.AssociationName;
                        }
                        else
                        {
                            Association supervisorAss = await _associationRepository.FindOneAsync(a => a.Id == bid.SupervisingAssociationId.Value);
                            model.SupervisorName = supervisorAss is null ? "" : supervisorAss.Association_Name;
                        }
                    }
                }
                else if (bid.EntityType == UserType.Donor)
                {
                    Donor donorCreatedbid = await _donorRepository.FindOneAsync(don => bid.EntityId == don.Id);
                    if (donorCreatedbid is null)
                        return OperationResult<ReadOnlyBidResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.DONOR_NOT_FOUND);

                    model.Entity_Image = await _imageService.GetFileResponseEncrypted(donorCreatedbid.Image, donorCreatedbid.ImageFileName);
                    model.Entity_Name = donorCreatedbid.DonorName;
                    model.EntityId = bid.EntityId;
                    model.EntityType = bid.EntityType;
                    await MapBidCreatorDetailsObjectIfDonor(user, bid, model);
                    //============check supervising=========================
                    model.SupervisorName = bid.BidDonorId.HasValue ? bid.BidDonor.DonorId.HasValue ? bid.BidDonor.Donor.DonorName : bid.BidDonor.NewDonorName : "";
                }
                else
                    throw new ArgumentException($"This Enum Value {bid.EntityType.ToString()} wasn't handled Here {nameof(IBidService.GetDetailsForBidByIdAsync)}");

                await MapBidReview(bid, model);
                model.NonAnsweredInquiriesCount = await _inquiryRepository.GetCountAsync(inq => inq.BidId == bidId && inq.InquiryStatus == InquiryStatus.NoResponse
                && !inq.IsHidden, false);
                model.ExtensionSuggestionsCount = await _providerBidExtensionRepository.GetCountAsync(sug => sug.BidId == bidId);
                model.NonEvaluatedOffersCount = await _bidsOfProviderRepository.GetTenderQuotationsNotReviewedCounter(bidId);

                if (bid.BidAddressesTime != null)
                    model.lastDateInOffersSubmission = bid.BidAddressesTime.LastDateInOffersSubmission != null ?  bid.BidAddressesTime?.LastDateInOffersSubmission.Value : null;

                model.BidStatusName = bid.BidStatus?.NameAr;
                model.BidVisibility = (BidTypes)bid.BidTypeId;
                model.BidVisibilityName = bid.BidType.NameAr;
                model.Ref_Number = bid.Ref_Number;

                var bidQuotations = await _tenderSubmitQuotationRepository
                    .Find(x => x.BidId == bid.Id && x.ProposalStatus == ProposalStatus.Delivered, false)
                    .GroupBy(x => x.BidId)
                    .Select(x => new
                    {
                        x.Key,
                        TenderQuotationsCount = x.Count(),
                        TenderQuotations = x.Select(q => new { q.Id, q.CompanyId,q.FreelancerId ,q.ReviewStatus, q.TechnicalReviewStatus }).ToList()
                    })
                    .FirstOrDefaultAsync();
                // model.TenderQuotationsCount = bidQuotations is not null && user.UserType != UserType.Provider ? bidQuotations.TenderQuotationsCount : 0;
                if (bidQuotations is not null)
                    model.TenderQuotationsCount = (user.UserType != UserType.Provider)
                || (user.UserType == UserType.Provider && (bid.BidTypeId == (int)BidTypes.Instant) && bid.isLimitedOffers == true)
                || (user.UserType == UserType.Freelancer && (bid.BidTypeId == (int)BidTypes.Freelancing) && bid.isLimitedOffers == true)?
                    bidQuotations.TenderQuotationsCount : null;

                var bidTermsBookBuyers = await _helperService.GetBidTermsBookBuyersDataAsync(bid);

                var providerBids = await _providerBidRepository
                                   .Find(x => x.BidId == bid.Id && x.IsPaymentConfirmed)
                                   .GroupBy(x => x.BidId)
                                   .Select(pb => new
                                   {
                                       providerBidsCount = pb.Count(),
                                       CurrentParticiantIsBuyRFI = company != null ? 
                                       pb.Any(x => x.CompanyId == company.Id && x.IsPaymentConfirmed) :
                                       freelancer!=null? pb.Any(x => x.FreelancerId == freelancer.Id && x.IsPaymentConfirmed): false
                                   }).FirstOrDefaultAsync();
                if (providerBids is not null && user.UserType != UserType.Provider&& user.UserType != UserType.Freelancer)
                {
                    model.TotalBidDocumentsPrice = providerBids.providerBidsCount * bid.Association_Fees;
                    model.TotalBidDocumentsCount = providerBids.providerBidsCount;
                }

                var bidWorkingSectors = bid.GetBidWorkingSectors();
                model.BidMainClassificationIds = bidWorkingSectors.Select(a => new BidMainClassificationIds { Id = a.Id, ParentId = a.ParentId }).ToList();
                model.BidMainClassificationNames = bidWorkingSectors.Select(i => new BidMainClassificationNames { Name = i.NameAr, ParentName = i.Parent?.NameAr }).ToList();

                if (user.UserType == UserType.Provider|| user.UserType == UserType.Freelancer)
                {
                   
                        if (providerBids is not null && providerBids.CurrentParticiantIsBuyRFI)
                        {
                            model.IsBuyRFI = true;
                            model.OfferStatusId = OfferStatus.NoOfferDelivered;
                        }
                        else
                        {
                            model.IsBuyRFI = false;
                            model.OfferStatusId = OfferStatus.CantSubmitOffer;
                        }
                    if (bidQuotations is not null &&
                    (
                    (company != null && bidQuotations.TenderQuotations.Any(c => c.CompanyId == company.Id))
                    ||
                    (freelancer != null && bidQuotations.TenderQuotations.Any(c => c.FreelancerId == freelancer.Id))
                    ))
                    {
                        var quotation = company!=null?
                            bidQuotations.TenderQuotations.FirstOrDefault(c => c.CompanyId == company.Id):
                            bidQuotations.TenderQuotations.FirstOrDefault(c => c.FreelancerId == freelancer.Id)
                            ;
                            model.IsApplyForBid = true;
                            model.QuotationId = quotation.Id;
                            model.QuotationReviewStatus = quotation.ReviewStatus;
                            model.QuotationReviewStatusName = model.QuotationReviewStatusName = quotation.ReviewStatus == 0
                                ? string.Empty
                                : EnumArabicNameExtensions.GetArabicNameFromEnum(quotation.ReviewStatus);
                            model.TechnicalReviewStatus = quotation.TechnicalReviewStatus;
                            switch (quotation.TechnicalReviewStatus)
                            {
                                case TechnicalReviewStatus.Accepted:
                                    model.TechnicalReviewStatusName = "مقبول";
                                    model.OfferStatusId = OfferStatus.AcceptOffer;
                                    break;

                                case TechnicalReviewStatus.Rejected:
                                    model.TechnicalReviewStatusName = "مرفوض";
                                    model.OfferStatusId = OfferStatus.RejectOffer;
                                    break;

                                case TechnicalReviewStatus.NotYetReviewed:
                                    model.TechnicalReviewStatusName = "لم يتم المراجعة";
                                    model.OfferStatusId = OfferStatus.OfferDelivered;
                                    break;

                                default:
                                    model.TechnicalReviewStatusName = String.Empty;
                                    model.OfferStatusId = OfferStatus.OfferDelivered;
                                    break;
                            }
                        }
                    
                }

                var awardingSelect = await _awardingSelectRepository.FindOneAsync(x => x.BidId == bid.Id, false, nameof(AwardingSelect.AwardingProviders));
                if (awardingSelect != null)
                    await MapAwardinData(bid, model, company, awardingSelect,freelancer);


                // return conatract id if bid has contract
                var contract = await _contractRepository.Find(a => a.TenderId == bid.Id && !a.IsDeleted && a.IsPublished, false)
                    .Select(c => new { c.Id, c.ContractStatus }).FirstOrDefaultAsync();
                if (contract is not null)
                {
                    model.ContractId = contract.Id;
                    model.ContractStatus = contract.ContractStatus;
                }
                model.ISEditable = CheckIfBidIsEditable(bid, user);

                await FillEvaluationData(bid, model);
                if (CheckIfCurrentUserIsCreator(user, model))
                    await MapCancelRequestStatus(bid, model);


                // Increase View Here To Reduce Mutible Calling Endpoints
                if (bid.BidStatusId.Value != (int)TenderStatus.Draft && bid.BidStatusId.Value != (int)TenderStatus.Reviewing)
                {
                    var increaseBidViewsResult = await IncreaseBidViewCountNew(bid, user);
                    if (!increaseBidViewsResult.IsSucceeded)
                        return OperationResult<ReadOnlyBidResponse>.Fail(increaseBidViewsResult.HttpErrorCode, increaseBidViewsResult.Code, increaseBidViewsResult.ErrorMessage);
                    model.ViewsCount = increaseBidViewsResult.Data;
                }

                // Get Bid Status Here To Reduce Mutible Calling Endpoints
                var getBidStatusResult = await GetBidStatusWithDatesNew(bid);
                if (!getBidStatusResult.IsSucceeded)
                    return OperationResult<ReadOnlyBidResponse>.Fail(getBidStatusResult.HttpErrorCode, getBidStatusResult.Code);
                model.BidStatuseTimeLine = getBidStatusResult.Data;

                model.BidReviewDetailsResponse = (bid.BidStatusId == (int)TenderStatus.Reviewing) ? null : (await _reviewedSystemRequestLogService
                    .GetReviewedSystemRequestLogAsync(new GetReviewedSystemRequestLogRequest { EntityId = bidId, SystemRequestType = SystemRequestTypes.BidReviewing })).Data;

                var ignoreTimeline = await _demoSettingsService.GetIgnoreTimeLineAsync();
                var shouldAppearCompanySensitiveDataAtBid = _helperService.ShouldAppearCompanySensitiveDataAtBid(bid, ignoreTimeline, user);
                if (shouldAppearCompanySensitiveDataAtBid)
                {

                    var lastProviderBid = await _providerBidRepository
                        .Find(x => x.BidId == bidId && x.IsPaymentConfirmed)
                        .Include(x => x.Company)
                        .Include(x => x.ManualCompany)
                        .Include(x => x.Freelancer)
                        .OrderByDescending(x => x.CreationDate)
                        .FirstOrDefaultAsync();
                    if (lastProviderBid != null)
                    {
                        model.LastCompanyBoughtDocs = new BasicInfoForCompanyResponse()
                        {
                            Id =  lastProviderBid.Company?.Id ?? lastProviderBid.Freelancer?.Id ?? lastProviderBid.ManualCompany?.Id ?? 0,
                            CompanyName = lastProviderBid.Company?.CompanyName ?? lastProviderBid.Freelancer?.Name ?? lastProviderBid.ManualCompany?.CompanyName ?? string.Empty,
                            Date = lastProviderBid?.CreationDate,
                            Logo = await _imageService.GetFileResponseEncrypted(lastProviderBid?.Company?.Image
                                                    ?? lastProviderBid.Freelancer?.ProfileImageFilePath
                                                    ?? lastProviderBid.ManualCompany?.Image ?? string.Empty)
                        };
                    }
                    var bestOfferInPrice = await _tenderSubmitQuotationRepository.Find(x => x.BidId == bidId && x.ProposalStatus == ProposalStatus.Delivered)
                        .Where(x => x.ReviewStatus != ReviewStatus.Rejected)
                        .Include(x => x.Company)
                        .Include(x => x.Freelancer)
                        .Include(x => x.ManualCompany)
                        .OrderBy(x => x.TotalAfterDiscount)
                        .FirstOrDefaultAsync();
                    if (bestOfferInPrice != null)
                    {
                      //  var manualComp = bestOfferInPrice.ManualCompanyId is not null ? await _manualCompanyRepository
                      //.Find(c => c.Id == bestOfferInPrice.ManualCompanyId, false)
                      //.FirstOrDefaultAsync() : null;
                        model.BestOfferBasedOnPrice = new BasicInfoForCompanyResponse()
                        {
                            Id = bestOfferInPrice.Id,
                            CompanyName = bestOfferInPrice.Company?.CompanyName ?? bestOfferInPrice.ManualCompany?.CompanyName ??bestOfferInPrice.Freelancer?.Name ?? string.Empty,
                            Date = bestOfferInPrice.CreationDate,
                            Logo = await _imageService.GetFileResponseEncrypted(bestOfferInPrice.Company?.Image?? bestOfferInPrice.Freelancer?.ProfileImageFilePath?? bestOfferInPrice.ManualCompany?.Image ?? null)
                        };
                    }
                }

                if (bid.BidStatusId.Value == (int)TenderStatus.Closed)
                {
                    var averageOffers = await _tenderSubmitQuotationRepository
                 .Find(x => x.ProposalStatus == ProposalStatus.Delivered
                 && x.Bid.BidStatusId == (int)TenderStatus.Closed &&
                 x.BidId == bid.Id, true, false)
                 .Select(s => new
                 {
                     PurchaseAmount = (double)s.TotalAfterDiscount
                 }).AverageAsync(x => x.PurchaseAmount);


                    var allAwarding = _awardingProviderRepository
                        .Find(x => x.ProviderStatus == ProviderStatus.Approved &&
                        x.AssociationStatus == AssociationStatus.Approved && x.AwardingSelect.BidId == bid.Id, true, false)
                        .Select(s => new
                        {
                            PurchaseAmount = (double)s.AwardingValue
                        });


                    //  .Concat(allmanualInvoices);
                    var allAwardingSum = await allAwarding.SumAsync(x => (double)x.PurchaseAmount);
                    model.SaveMoney = (averageOffers - (allAwardingSum)) < 1000 ? 0 : averageOffers - (allAwardingSum);
                    model.SaveMoneyPercentage = model.SaveMoney < 1000 ? 0 : (model.SaveMoney / (averageOffers)) * 100;
                }
                return OperationResult<ReadOnlyBidResponse>.Success(model);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = $"bid Id = {bidId}",
                    ErrorMessage = "Failed to get Details For Bid By Id Async!",
                    ControllerAndAction = "BidController/bid-details/{id}"
                });
                return OperationResult<ReadOnlyBidResponse>.Fail(HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }

        }
        private async Task MapRevealsData(ApplicationUser user, Bid bid, ReadOnlyBidResponse model)
        {
            var userType = user.UserType == UserType.Provider ? UserType.Company : user.UserType;
            var isFeaturesEnabled = await _appGeneralSettingsRepository
                .Find(x => true).Select(x => x.IsSubscriptionFeaturesEnabled)
                .FirstOrDefaultAsync();

            if (!isFeaturesEnabled)
            {
                model.RevealsCount = new RevealsCountResponse()
                {
                    IsRevealedThisBidBefore = true
                };
                return;
            }

            var subscriptionPaymentFeatureUsage = await _subscriptionPaymentFeatureUsageRepository
             .Find(x => x.subscriptionPaymentFeature.SubscriptionPayment.IsPaymentConfirmed &&
             x.subscriptionPaymentFeature.SubscriptionPayment.UserId == user.CurrentOrgnizationId
             && x.subscriptionPaymentFeature.SubscriptionPayment.UserTypeId == userType)
             .Where(x => x.BidId == bid.Id)
             .Include(x => x.subscriptionPaymentFeature.SubscriptionPayment.SubscriptionPackagePlan)
             .AsSplitQuery()
             .FirstOrDefaultAsync();

            if (subscriptionPaymentFeatureUsage is not null)
            {

                var FeatureUsage = subscriptionPaymentFeatureUsage.subscriptionPaymentFeature?.SubscriptionPayment.SubscriptionPaymentFeatures.FirstOrDefault();
                var isPremiumPackage = (FeatureUsage != null && FeatureUsage.ValueType == FeatureValueType.Count &&
                     FeatureUsage.Count.HasValue && FeatureUsage.Count.Value == int.MaxValue) || (FeatureUsage != null && FeatureUsage.Count is null);

                model.RevealsCount = new RevealsCountResponse()
                {
                    IsRevealedThisBidBefore = true,
                    TotalRevealsCount = !isPremiumPackage ? subscriptionPaymentFeatureUsage.subscriptionPaymentFeature?.Count : null,
                    UsedRevealsCount = !isPremiumPackage ? subscriptionPaymentFeatureUsage.subscriptionPaymentFeature?.UsageCount : null,
                    IsAvailable = subscriptionPaymentFeatureUsage.subscriptionPaymentFeature?.IsAvailable,
                    ValueType = subscriptionPaymentFeatureUsage.subscriptionPaymentFeature?.ValueType,
                    PackageName = subscriptionPaymentFeatureUsage.subscriptionPaymentFeature?.SubscriptionPayment.SubscriptionPackagePlan.Name
                };
            }
            else
            {
                var subscriptionPayment = await _subscriptionPaymentRepository
                    .Find(x => !x.IsExpired && x.IsPaymentConfirmed && x.UserId == user.CurrentOrgnizationId && x.UserTypeId == userType)
                    .Include(x => x.SubscriptionPaymentFeatures.Where(x => x.ValueType == FeatureValueType.Count).Take(1))
                     .ThenInclude(x => x.Feature) 
                    .Include(x => x.SubscriptionPackagePlan)
                    .AsSplitQuery()
                    .OrderByDescending(x => x.CreationDate)
                    .FirstOrDefaultAsync();


                if (subscriptionPayment is not null)
                {
                    var firstFeature = subscriptionPayment.SubscriptionPaymentFeatures.FirstOrDefault();
                    var isPremiumPackage = (firstFeature != null && firstFeature.ValueType == FeatureValueType.Count &&
                         firstFeature.Count.HasValue && firstFeature.Count.Value == int.MaxValue) || (firstFeature != null && firstFeature.Count is null);

                    model.RevealsCount = new RevealsCountResponse()
                    {
                        IsRevealedThisBidBefore = isPremiumPackage || (userType != UserType.Company&&userType!= UserType.Freelancer) ? true : false,
                        TotalRevealsCount = firstFeature?.Count,
                        UsedRevealsCount = firstFeature?.UsageCount,
                        IsAvailable = firstFeature?.IsAvailable,
                        ValueType = firstFeature?.ValueType,
                        PackageName = subscriptionPayment.SubscriptionPackagePlan.Name
                    };

                    if (isPremiumPackage)
                    {
                        await CreatePremiumPackageUsageTracking(bid);
                    }
                }
                else
                {
                    model.RevealsCount = new RevealsCountResponse()
                    {
                        IsRevealedThisBidBefore = false
                    };
                }
            }
            if (userType != UserType.Company && userType != UserType.Freelancer)
                model.RevealsCount = new RevealsCountResponse()
                {
                    IsRevealedThisBidBefore = true
                };
        }

        private async Task CreatePremiumPackageUsageTracking(Bid bid)
        {
            using var scope = _serviceProvider.CreateScope();
            var _subscriptionsSettingsService = scope.ServiceProvider.GetRequiredService<ISubscriptionsSettingsService>();
            try
            {
                var featureType = (!string.IsNullOrEmpty(bid.Tender_Brochure_Policies_Url) ||
                   bid.BidAttachment?.Any() == true) ?
                  FeatureTypes.DownloadTermsBook :
                  FeatureTypes.ViewContactDetails;

                var result = await _subscriptionsSettingsService.RevealFeature(bid.Id, featureType);

                if (!result.IsSucceeded)
                {
                    _logger.Log(new LoggerModel
                    {
                        UserRequestModel = $"bid Id = {bid.Id}, ErrorCode = {result.Code}, HttpErrorCode = {result.HttpErrorCode}",
                        ErrorMessage = $"RevealFeature failed: {result.ErrorMessage}",
                        ControllerAndAction = "BidController/bid-details/{id}"
                    });
                }
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = $"bid Id = {bid.Id}",
                    ErrorMessage = "Failed to create premium package tracking for Bid By Id Async!",
                    ControllerAndAction = "BidController/bid-details/{id}"
                });
            }
        }

        //private async Task MapRevealsData(ApplicationUser user, Bid bid, ReadOnlyBidResponse model)
        //{
        //    var userType = user.UserType == UserType.Provider ? UserType.Company : user.UserType;
        //    var isFeaturesEnabled = await _appGeneralSettingsRepository
        //        .Find(x => true).Select(x => x.IsSubscriptionFeaturesEnabled)
        //        .FirstOrDefaultAsync();
        //    if (!isFeaturesEnabled)
        //    {
        //        model.RevealsCount = new RevealsCountResponse()
        //        {
        //            IsRevealedThisBidBefore = true
        //        };
        //        return;
        //    }

        //    var subscriptionPaymentFeatureUsage = await _subscriptionPaymentFeatureUsageRepository
        //     .Find(x => x.subscriptionPaymentFeature.SubscriptionPayment.IsPaymentConfirmed &&
        //     x.subscriptionPaymentFeature.SubscriptionPayment.UserId == user.CurrentOrgnizationId
        //     && x.subscriptionPaymentFeature.SubscriptionPayment.UserTypeId == userType)
        //     .Where(x => x.BidId == bid.Id)
        //     .Include(x => x.subscriptionPaymentFeature.SubscriptionPayment.SubscriptionPackagePlan)
        //     .AsSplitQuery()
        //     .FirstOrDefaultAsync();
        //    // return OperationResult<ReadOnlyBidResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);
        //    if (subscriptionPaymentFeatureUsage is not null)
        //    {
        //        model.RevealsCount = new RevealsCountResponse()
        //        {
        //            IsRevealedThisBidBefore = true,
        //            TotalRevealsCount = subscriptionPaymentFeatureUsage.subscriptionPaymentFeature?.Count,
        //            UsedRevealsCount = subscriptionPaymentFeatureUsage.subscriptionPaymentFeature?.UsageCount,
        //            IsAvailable = subscriptionPaymentFeatureUsage.subscriptionPaymentFeature?.IsAvailable,
        //            ValueType = subscriptionPaymentFeatureUsage.subscriptionPaymentFeature?.ValueType,
        //            PackageName = subscriptionPaymentFeatureUsage.subscriptionPaymentFeature?.SubscriptionPayment.SubscriptionPackagePlan.Name
        //        };
        //    }
        //    else
        //    {
        //        var subscriptionPayment = await _subscriptionPaymentRepository
        //            .Find(x => !x.IsExpired && x.IsPaymentConfirmed && x.UserId == user.CurrentOrgnizationId && x.UserTypeId == userType)
        //            .Include(x => x.SubscriptionPaymentFeatures.Where(x => x.ValueType == FeatureValueType.Count).Take(1))
        //            .Include(x => x.SubscriptionPackagePlan)
        //            .AsSplitQuery()
        //            .OrderByDescending(x => x.CreationDate)
        //            .FirstOrDefaultAsync();
        //        // return OperationResult<ReadOnlyBidResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);
        //        if (subscriptionPayment is not null)
        //        {
        //            var firstFeature = subscriptionPayment.SubscriptionPaymentFeatures.FirstOrDefault();
        //            var isPremiumPackage = (firstFeature != null && firstFeature.ValueType == FeatureValueType.Count &&
        //                firstFeature.Count.HasValue && firstFeature.Count.Value == int.MaxValue) || (firstFeature != null && firstFeature.Count is null);


        //            model.RevealsCount = new RevealsCountResponse()
        //            {
        //                IsRevealedThisBidBefore = isPremiumPackage || (userType != UserType.Company) ? true : false,
        //                TotalRevealsCount = firstFeature?.Count,
        //                UsedRevealsCount = firstFeature?.UsageCount,
        //                IsAvailable = firstFeature?.IsAvailable,
        //                ValueType = firstFeature?.ValueType,
        //                PackageName = subscriptionPayment.SubscriptionPackagePlan.Name
        //            };
        //            if (isPremiumPackage)
        //            {
        //                await _bidRevealLogRepository.Add(new BidRevealLog
        //                {
        //                    BidId = bid.Id,
        //                    SubscriptionPaymentId = subscriptionPayment.Id,
        //                    Status = BidRevealStatus.RevealedViaPackage,
        //                    CompanyId = userType == UserType.Company ? user.CurrentOrgnizationId : null,
        //                });
        //            }
        //        }
        //        else
        //        {
        //            model.RevealsCount = new RevealsCountResponse()
        //            {
        //                IsRevealedThisBidBefore = false
        //            };
        //        }
        //    }
        //    if (userType != UserType.Company)
        //        model.RevealsCount = new RevealsCountResponse()
        //        {
        //            IsRevealedThisBidBefore = true
        //        };
        //}

        private bool CheckIfWeShouldNotShowProviderData(Bid bid, bool ignoreTimeLine)
        {
            var currentUser = _currentUserService.CurrentUser;
            if (Constants.AdminstrationUserTypes.Contains(currentUser.UserType))
                return false;
            else if (currentUser.UserType != UserType.Association && currentUser.UserType != UserType.Donor)
                return true;

            if (((currentUser.UserType == UserType.Association || currentUser.UserType == UserType.Donor) &&
                        (bid.EntityId != currentUser.CurrentOrgnizationId || bid.EntityType != currentUser.UserType)))
                return true;

            if (ignoreTimeLine)
                return false;

            return bid.BidStatusId != (int)TenderStatus.Closed && bid.BidStatusId != (int)TenderStatus.Awarding &&
                bid.BidStatusId != (int)TenderStatus.Stopping;
        }

        public async Task<OperationResult<List<GetReviewedSystemRequestLogResponse>>> GetProviderInvitationLogs(long bidId)
        {
            try
            {

                var user = _currentUserService.CurrentUser;

                if (!Constants.AdminstrationUserTypes.Contains(user.UserType))
                    return OperationResult<List<GetReviewedSystemRequestLogResponse>>.Fail(HttpErrorCode.NotAuthorized, CommonErrorCodes.NotAuthorized);


                var invitationLogs = await _reviewedSystemRequestLogService
                    .GetMultibleReviewedSystemRequestLogsAsync(new List<long>() { bidId }, SystemRequestTypes.BidInviting);
                var bidInvitationsLogs = (invitationLogs.IsSucceeded ? invitationLogs.Data : new List<GetReviewedSystemRequestLogResponse>());
                return OperationResult<List<GetReviewedSystemRequestLogResponse>>.Success(bidInvitationsLogs);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = $"bid Id = {bidId}",
                    ErrorMessage = "Get Provider Invitation Logs!",
                    ControllerAndAction = "BidController/GetProviderInvitationLogs/{id}"
                });
                return OperationResult<List<GetReviewedSystemRequestLogResponse>>.Fail(HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }

        private async Task<bool> CheckIfBidForAssignedComapniesOnly(Bid bid, Company company)
        {
            return bid.IsBidAssignedForAssociationsOnly
                && company.AssignedAssociationId is null
                && company.AssignedDonorId is null
                && !await _providerBidRepository
                .Find(a => a.IsPaymentConfirmed && a.CompanyId == company.Id && a.BidId == bid.Id)
                .AnyAsync();
        }

        private async Task MapBidCreatorDetailsIfAssociation(ApplicationUser user, Bid bid, ReadOnlyBidResponse model)
        {
            model.BidCreatorDetails = user.UserType == UserType.SuperAdmin ||
                user.UserType == UserType.Admin ? new BidCreatorDetailsResponse()
                {
                    RegisterationNumber = bid.Association.Registry_Number,
                    RegisterationNumberEndDate = bid.Association.Registry_ExpiryDate,
                    DelegateFile = await _imageService.GetFileResponseEncrypted(bid.Association.DelegationFile, bid.Association.DelegationFileFileName),
                    RegisteryFile = await _imageService.GetFileResponseEncrypted(bid.Association.Attachment, bid.Association.AttachmentFileName)

                } : null;
        }

        private async Task MapBidCreatorDetailsObjectIfDonor(ApplicationUser user, Bid bid, ReadOnlyBidResponse model)
        {
            model.BidCreatorDetails = user.UserType == UserType.SuperAdmin ||
                user.UserType == UserType.Admin ? new BidCreatorDetailsResponse()
                {
                    RegisterationNumber = bid.Donor.RegistryNumber,
                    RegisterationNumberEndDate = bid.Donor.RegistryExpiryDate,
                    DelegateFile = await _imageService.GetFileResponseEncrypted(bid.Donor.DelegationFile, bid.Donor.DelegationFileFileName),
                    RegisteryFile = await _imageService.GetFileResponseEncrypted(bid.Donor.RegistryAttachment, bid.Donor.RegistryAttachmentFileName)

                } : null;
        }

        private async Task MapBidReview(Bid bid, ReadOnlyBidResponse model)
        {
            var user = _currentUserService.CurrentUser;
            if (!CheckIfUserCanViewLog(bid, model, user))
                return;
            var reviewLog = await _reviewedSystemRequestLogService.GetReviewedSystemRequestLogAsync(new() { EntityId = bid.Id, SystemRequestType = SystemRequestTypes.BidReviewing });
            model.BidReviewDetailsResponse = reviewLog.Data;
        }

        private static bool CheckIfUserCanViewLog(Bid bid, ReadOnlyBidResponse model, ApplicationUser user)
        {
            var isCreator = (user.UserType == bid.EntityType && user.CurrentOrgnizationId == bid.EntityId);
            var isDonorIsSponsor = (user.UserType == UserType.Donor &&
                model.SponsorDonorId == user.CurrentOrgnizationId && model.donorResponse != DonorResponse.Reject);
            return (Constants.AdminstrationUserTypes.Contains(user.UserType)) || isCreator || isDonorIsSponsor;


        }

        private async Task<OperationResult<ReadOnlyBidResponse>> MapPublicData(long bidId)
        {
            var bid = await GetBidWithRelatedEntitiesByIdAsync(bidId);

            if (bid is null)
                return OperationResult<ReadOnlyBidResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);
            if (bid.BidStatusId == (int)TenderStatus.Draft
                || bid.BidStatusId == (int)TenderStatus.Pending
                || bid.BidStatusId == (int)TenderStatus.Reviewing)
                return OperationResult<ReadOnlyBidResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);

            var model = _mapper.Map<ReadOnlyBidResponse>(bid);


            if (bid.BidStatusId == (int)TenderStatus.Closed)
            {
                model.IsBidAwarded = true;
                model.CompanyEvaluation = null;
                model.EntityEvaluation = null;
            }

            ReturnDistinctSupervisingDataBasedOnClaimType(model);

            model.InvitedAssociationByDonor = await this.GetInvitedAssociationIfFound(bid);
            model.Regions = BidRegion.getAllRegionsAsListOfIds(bid.BidRegions);
            model.RegionsNames = bid.BidRegions.Select(b => b.Region.NameAr).ToList();
            int regionCount = await _regionRepository.Find(a => true, true, false).CountAsync();
            if (model.RegionsNames.Count == regionCount)
            {
                model.RegionsNames.Clear();
                model.RegionsNames.Add(Constants.AllRegionsArabic);
            }


            //=============== check is bid supervised by donor=============

            if (bid.BidAddressesTime != null)
                model.lastDateInOffersSubmission = (DateTime)bid.BidAddressesTime.LastDateInOffersSubmission;

            model.BidStatusName = bid.BidStatus?.NameAr;
            model.BidVisibility = (BidTypes)bid.BidTypeId;
            model.BidVisibilityName = bid.BidType.NameAr;
            model.Ref_Number = bid.Ref_Number;

            var bidWorkingSectors = bid.GetBidWorkingSectors();
            model.BidMainClassificationIds = bidWorkingSectors.Select(a => new BidMainClassificationIds { Id = a.Id, ParentId = a.ParentId }).ToList();
            model.BidMainClassificationNames = bidWorkingSectors.Select(i => new BidMainClassificationNames { Name = i.NameAr, ParentName = i.Parent?.NameAr }).ToList();

            //not show for anynumous users
            //var awardingSelect = await _awardingSelectRepository.FindOneAsync(x => x.BidId == bid.Id, false, nameof(AwardingSelect.AwardingProviders));
            //if (awardingSelect != null)
            //    await MapAwardinData(bid, model, null, awardingSelect,null);


            // return conatract id if bid has contract
            var contractId = await _contractRepository.Find(a => a.TenderId == bid.Id && !a.IsDeleted && a.IsPublished, false)
                .Select(c => c.Id).FirstOrDefaultAsync();
            if (contractId != default)
                model.ContractId = contractId;
            //not show for anynumous users
            //await FillEvaluationData(bid, model);

            // Increase View Here To Reduce Mutible Calling Endpoints
            if (bid.BidStatusId.Value != (int)TenderStatus.Draft)
            {
                var increaseBidViewsResult = await IncreaseBidViewCountNew(bid, null);
                if (!increaseBidViewsResult.IsSucceeded)
                    return OperationResult<ReadOnlyBidResponse>.Fail(increaseBidViewsResult.HttpErrorCode, increaseBidViewsResult.Code, increaseBidViewsResult.ErrorMessage);
                model.ViewsCount = increaseBidViewsResult.Data;
            }

            if (bid.EntityType == UserType.Association)
            {
                model.Entity_Image = await _imageService.GetFileResponseEncrypted(bid.Association.Image, bid.Association.ImageFileName);
                model.Entity_Name = bid.Association.Association_Name;
                model.EntityType = bid.EntityType;
                model.EntityId = bid.EntityId;
            }
            else if (bid.EntityType == UserType.Donor)
            {
                model.Entity_Image = await _imageService.GetFileResponseEncrypted(bid.Donor.Image, bid.Donor.ImageFileName);
                model.Entity_Name = bid.Donor.DonorName;
                model.EntityType = bid.EntityType;
                model.EntityId = bid.EntityId;
                //}
            }


            return OperationResult<ReadOnlyBidResponse>.Success(model);
        }

        private async Task MapAwardinData(Bid bid, ReadOnlyBidResponse model, Company company, AwardingSelect awardingSelect, Freelancer freelancer)
        {
            var awardingProvider = awardingSelect.AwardingProviders.OrderByDescending(a => a.CreationDate).FirstOrDefault();
            if (awardingProvider != null)
            {

                var awardedCompany = awardingProvider.CompanyId is not null
                    ? await _companyRepository.Find(c => c.Id == awardingProvider.CompanyId, false)
                    .Select(x => new { Id = x.Id, Name = x.CompanyName, manualRegistered = x.CompanyRegistrationStatus, UserType = UserType.Company })
                    .FirstOrDefaultAsync()
                    : awardingProvider.ManualCompanyId is not null
                    ? await _manualCompanyRepository.Find(c => c.Id == awardingProvider.ManualCompanyId, false)
                    .Select(x => new { Id = x.Id, Name = x.CompanyName, manualRegistered = RegistrationStatus.ManualRegistered, UserType = UserType.ManualCompany })
                    .FirstOrDefaultAsync()
                    : await _freelancerRepository
                    .Find(c => c.Id == awardingProvider.FreelancerId, false)
                    .Select(x => new { x.Id, x.Name, manualRegistered = x.RegistrationStatus, UserType = UserType.Freelancer })
                    .FirstOrDefaultAsync();


                if (awardingProvider.ProviderStatus == ProviderStatus.Approved
                && awardingProvider.AssociationStatus == AssociationStatus.Approved)
                {
                    // awarded
                    model.IsBidAwarded = true;
                    model.AwardedCompanyId = awardedCompany != null ? awardedCompany.Id : 0;
                    model.AwardedCompanyType = awardedCompany.UserType;
                    model.AwardingValue = awardingProvider.AwardingValue;
                    model.CompanyRegistrationStatus = awardedCompany.manualRegistered;
                    //  model.AdditionalDiscount = quotation.a
                    model.AwardedCompany = awardedCompany != null ? awardedCompany.Name : String.Empty;

                    if ((company != null && awardedCompany != null) && (awardedCompany.Id == company.Id))
                    {
                        model.IsAwardedByCurrentUser = true;
                    }
                    else if ((freelancer != null && awardedCompany != null) && (awardedCompany.Id == freelancer.Id))
                    {
                        model.IsAwardedByCurrentUser = true;
                    }
                    var tenderSubmitQuestion = await _tenderSubmitQuotationRepository.FindOneAsync(a =>
                     (a.CompanyId == awardedCompany.Id || a.ManualCompanyId == awardedCompany.Id || a.FreelancerId == awardedCompany.Id)
                     && a.BidId == bid.Id);
                    model.AdditionalDiscount = tenderSubmitQuestion.AdditionalDiscount;
                    model.TotalAfterDiscount = tenderSubmitQuestion.TotalAfterDiscount;
                }

                var quantitiesTable = await _providerQuantitiesTableDetailsRepository.FindOneAsync(q => q.BidId == bid.Id
                && (q.CompanyId == awardingProvider.CompanyId || q.ManualCompanyId == awardingProvider.ManualCompanyId || q.FreelancerId == awardingProvider.FreelancerId));
                if (((company != null && awardingProvider.CompanyId == company.Id) || (freelancer != null && awardingProvider.FreelancerId == freelancer.Id))
                    && quantitiesTable != null
                    && (decimal)quantitiesTable.TotalPrice != awardingProvider.AwardingValue
                    && awardingProvider.ProviderStatus == ProviderStatus.Pending
                    && awardingProvider.AssociationStatus == AssociationStatus.Approved)
                {
                    model.IsQuantitiesTableNeedEditing = true;
                    model.IsTherePendingAwarding = true;
                }

                if (quantitiesTable != null
                    && (decimal)quantitiesTable.TotalPrice != awardingProvider.AwardingValue
                    && awardingProvider.ProviderStatus == ProviderStatus.Pending
                    && awardingProvider.AssociationStatus == AssociationStatus.Approved)
                {
                    model.IsTherePendingAwarding = true;
                }

                if (
                    awardingProvider.IsDiscountRequestedByAssociation
                    && awardingProvider.ProviderStatus == ProviderStatus.Pending
                    && awardingProvider.AssociationStatus == AssociationStatus.Approved
                )
                {
                    model.IsTherePendingDiscountRequest = true;
                }
            }
        }

        private static bool CheckIfCurrentUserIsCreator(ApplicationUser user, ReadOnlyBidResponse model)
        {
            return user.CurrentOrgnizationId == model.EntityId && user.UserType == model.EntityType;
        }

        private async Task MapCancelRequestStatus(Bid bid, ReadOnlyBidResponse model)
        {
            var lastCancelRequest = await
                 _cancelBidRequestRepository
                 .Find(x => x.BidId == bid.Id)
                 .OrderByDescending(x => x.CreationDate)
                 .FirstOrDefaultAsync();
            model.CancelBidRequestStatus = lastCancelRequest is null ? null : lastCancelRequest.CancelBidRequestStatus;
            model.CancelBidRequestDate = lastCancelRequest is null ? null : lastCancelRequest.CreationDate;
        }

        private async Task FillEvaluationData(Bid bid, ReadOnlyBidResponse model)
        {
            // هنا بجيب التقييم اللى اتعمل للمورد من قبل الجمعيه   
            var entityEvaluation = await _evaluationRepository
                                .Find(a =>
                                a.BidId == bid.Id
                                && (a.UnderRatingUserType == UserType.Company || a.UnderRatingUserType==UserType.Freelancer), true, false)
                                .Select(a => new EvaluationForBidDto
                                {
                                    Id = a.Id,
                                    Status = a.Status,
                                    ServiceRating = a.ServiceRating,
                                    ServiceRatingNote = a.ServiceRatingNote,
                                    EvaluationItems = a.EvaluationItems
                                    .Select(x => new EvaluationItemDetailsDto
                                    {
                                        Id = x.RatingCriteria.Id,
                                        Name = x.RatingCriteria.Name,
                                        Type = x.RatingCriteria.Type,
                                        ItemRate = x.ItemRate
                                    })
                                    .ToList()
                                })
                                .FirstOrDefaultAsync();
            if (entityEvaluation is not null && entityEvaluation.Status == RatingRequestStatus.Approved)
                model.EntityEvaluation = entityEvaluation;
            else if (entityEvaluation is not null && entityEvaluation.Status != RatingRequestStatus.Approved)
            {
                model.EntityEvaluation = new EvaluationForBidDto();
                model.EntityEvaluation.Id = entityEvaluation.Id;
                model.EntityEvaluation.Status = entityEvaluation.Status;
            }


            //هنا بجيب التقييم اللى اتعمل للجمعيه او المانح من قبل المورد  
            var companyEvaluation = await _evaluationRepository
                                .Find(a =>
                                a.BidId == bid.Id
                                && (a.UnderRatingUserType == UserType.Association || a.UnderRatingUserType == UserType.Donor), true, false)
                                .Select(a => new EvaluationForBidDto
                                {
                                    Id = a.Id,
                                    Status = a.Status,
                                    ServiceRating = a.ServiceRating,
                                    ServiceRatingNote = a.ServiceRatingNote,
                                    EvaluationItems = a.EvaluationItems
                                    .Select(x => new EvaluationItemDetailsDto
                                    {
                                        Id = x.RatingCriteria.Id,
                                        Name = x.RatingCriteria.Name,
                                        Type = x.RatingCriteria.Type,
                                        ItemRate = x.ItemRate
                                    })
                                    .ToList()
                                })
                                .FirstOrDefaultAsync();
            if (companyEvaluation is not null && companyEvaluation.Status == RatingRequestStatus.Approved)
                model.CompanyEvaluation = companyEvaluation;
            else if (companyEvaluation is not null && companyEvaluation.Status != RatingRequestStatus.Approved)
            {
                model.CompanyEvaluation = new EvaluationForBidDto();
                model.CompanyEvaluation.Id = companyEvaluation.Id;
                model.CompanyEvaluation.Status = companyEvaluation.Status;
            }
            //if (companyEvaluation is not null)
            //    model.CompanyEvaluation = companyEvaluation;
        }

        private void ReturnDistinctSupervisingDataBasedOnClaimType(ReadOnlyBidResponse model)
        {
            if (model.BidSupervisingData.Any())
            {
                var distinctData = model.BidSupervisingData
                    .GroupBy(a => a.SupervisingServiceClaimCode)
                    .Select(a => a.OrderByDescending(a => a.Id).FirstOrDefault())
                    .ToList();

                model.BidSupervisingData = distinctData;
                model.BidSupervisingData.ForEach(a =>
                {
                    a.SponsorSupervisingStatusName = a.SupervisorStatus.HasValue ? EnumArabicNameExtensions.GetArabicNameFromEnum(a.SupervisorStatus.Value) : null;
                    a.SupervisingServiceClaimCodesName = EnumArabicNameExtensions.GetArabicNameFromEnum(a.SupervisingServiceClaimCode);
                });
            }
        }

        public async Task<OperationResult<long>> IncreaseBidViewCount(long bidId)
        {
            try
            {
                long count = 5;
                //=====================check Authorization=======================
                var user = _currentUserService.CurrentUser;
                if (user == null)
                    return OperationResult<long>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);

                //==========================check bid exist=================================
                var bid = await _bidRepository.FindOneAsync(x => x.Id == bidId && !x.IsDeleted, false);
                if (bid == null)
                    return OperationResult<long>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);

                if (user.UserType != UserType.Association && user.UserType != UserType.Provider && user.UserType != UserType.Company && user.UserType != UserType.Donor)
                {
                    count += bid.ViewsCount;
                    return OperationResult<long>.Success(count);
                }

                //====================get Current Organization====================
                Organization org = await _organizatioRepository.FindOneAsync(
                                   a => a.EntityID == user.CurrentOrgnizationId
                                   && a.OrgTypeID == (OrganizationType)user.OrgnizationType);
                if (org == null)
                    return OperationResult<long>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.THIS_ENTITY_HAS_NO_ORGNIZATION_RECORD);

                //=====================check is organization Already Exist===========================
                var bidViews = await _bidViewsLogRepository.FindAsync(a => a.BidId == bidId);

                if (bidViews.Any(a => a.OrganizationId == org.Id))
                {
                    count += bid.ViewsCount;
                    return OperationResult<long>.Success(count);
                }
                //====================Add new Organization View & Update on Bid Views Count===================================
                BidViewsLog request = new BidViewsLog
                {
                    OrganizationId = org.Id,
                    BidId = bidId,
                    SeenDate = _dateTimeZone.CurrentDate
                };
                await _bidViewsLogRepository.Add(request);

                //===========================update views count On Bid====================================
                bid.ViewsCount = bidViews.Count() + 1;
                await _bidRepository.Update(bid);

                //=============================return response==================================               
                count += bid.ViewsCount;
                return OperationResult<long>.Success(count);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = $"bid Id = {bidId}",
                    ErrorMessage = "Failed to increase views count for bid!",
                    ControllerAndAction = "BidController/increase-view-count/{id}"
                });
                return OperationResult<long>.Fail(HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }

        private async Task<OperationResult<long>> IncreaseBidViewCountNew(Bid bid, ApplicationUser user)
        {

            long count = 5;
            var bidViewsQuery =  _bidViewsLogRepository.Find(a => a.BidId == bid.Id);
            //=====================check Authorization=======================
            var bidViews = await bidViewsQuery.CountAsync();
            if (user == null)
            {
                await AddbidViewLog(bid, null, bidViews);

                return OperationResult<long>.Success(bid.ViewsCount + count);
            }

            //==========================check bid exist=================================
            if (bid == null)
                return OperationResult<long>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);

            if (user.UserType != UserType.Association && user.UserType != UserType.Provider && user.UserType != UserType.Freelancer && user.UserType != UserType.Company && user.UserType != UserType.Donor)
            {
                count += bid.ViewsCount;
                return OperationResult<long>.Success(count);
            }

            //====================get Current Organization====================
            Organization org = await _organizatioRepository.FindOneAsync(
                                a => a.EntityID == user.CurrentOrgnizationId
                                && a.OrgTypeID == (OrganizationType)user.OrgnizationType);
            if (org == null)
                return OperationResult<long>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.THIS_ENTITY_HAS_NO_ORGNIZATION_RECORD);

            //=====================check is organization Already Exist===========================

            if (await bidViewsQuery.AnyAsync(a => a.OrganizationId == org.Id))
            {
                count += bid.ViewsCount;
                return OperationResult<long>.Success(count);
            }
            //====================Add new Organization View & Update on Bid Views Count===================================
            await AddbidViewLog(bid, org, bidViews);

            //=============================return response==================================               
            count += bid.ViewsCount;
            return OperationResult<long>.Success(count);
        }

        private async Task AddbidViewLog(Bid bid, Organization org, int bidViewsCount)
        {
            BidViewsLog request = new BidViewsLog
            {
                OrganizationId = org?.Id,
                BidId = bid.Id,
                SeenDate = _dateTimeZone.CurrentDate
            };
            await _bidViewsLogRepository.ExexuteAsTransaction(async () =>
            {
                await _bidViewsLogRepository.Add(request);

                //===========================update views count On Bid====================================
                bid.ViewsCount = bidViewsCount + 1;
                await _bidRepository.Update(bid);
            });
        }

        public async Task<PagedResponse<List<GetMyBidResponse>>> GetMyBidsAsync(FilterBidsSearchModel model)
        {
            try
            {
                var usr = _currentUserService.CurrentUser;
                if (usr == null)
                    return new PagedResponse<List<GetMyBidResponse>>
                        (HttpErrorCode.NotAuthenticated);

                await _bidSearchLogService.AddBidSearcLogAsync(model, usr);

                List<GetMyBidResponse> models = new List<GetMyBidResponse>();

                if (usr.UserType == UserType.Provider && usr.OrgnizationType == (int)OrganizationType.Comapny)
                {
                    IQueryable<ProviderBid> providerBids = null;
                    Company currentCompany = null;
                    ApplicationUser currentCompanyUser = null;

                    currentCompany = await _companyRepository.FindOneAsync(x => x.IsDeleted == false && x.isVerfied == true && x.Id == usr.CurrentOrgnizationId, false, nameof(Company.Provider));
                    if (currentCompany is null)
                        return new PagedResponse<List<GetMyBidResponse>>
                            (HttpErrorCode.NotAuthorized, CommonErrorCodes.COMPANY_NOT_FOUND);

                    currentCompanyUser = await _userManager.FindByEmailAsyncSafe(currentCompany.Email);
                    if (currentCompanyUser == null)
                        return new PagedResponse<List<GetMyBidResponse>>
                            (HttpErrorCode.NotAuthorized, CommonErrorCodes.USER_NOT_EXIST);

                    providerBids = _providerBidRepository
                        .Find(x => x.CompanyId == currentCompany.Id && x.IsPaymentConfirmed && !x.Bid.IsDeleted, false);

                    if (currentCompany.AssignedAssociationId is null && currentCompany.AssignedDonorId is null)
                        providerBids = providerBids.Where(b => !b.Bid.IsBidAssignedForAssociationsOnly);

                    // الوثائق التي تم شرائها من قبل لمنافسات مسندة
                    var assignedForAssociationsOnlyProviderBidsAndThisCompanyBoughtIt = GetAssignedForAssociationsOnlyProviderBidsAndThisCompanyBoughtTermsBook(currentCompany.Id);

                    providerBids = providerBids
                        .Union(assignedForAssociationsOnlyProviderBidsAndThisCompanyBoughtIt)
                        .Include(p => p.Bid)
                            .ThenInclude(b => b.BidAddressesTime)
                        .Include(b => b.Bid.Association)
                        .Include(b => b.Bid.BidRegions)
                            .ThenInclude(c => c.Region)
                        .Include(b => b.Bid.BidStatus)
                        .Include(b => b.Bid.BidType)
                        .Include(b => b.Bid.Bid_Industries).ThenInclude(y => y.CommercialSectorsTree).ThenInclude(z => z.Parent)
                        .Include(b => b.Bid.FreelanceBidIndustries).ThenInclude(y => y.FreelanceWorkingSector).ThenInclude(z => z.Parent)
                        .Include(b => b.Bid.BidType)
                        .Include(x => x.Bid.Donor)
                        .AsSplitQuery();

                    providerBids = ApplyFiltrationForProviderBids(model, providerBids,usr.UserType);

                    int totalRecords = await providerBids.CountAsync();
                    if (providerBids == null || totalRecords == 0)
                        return new PagedResponse<List<GetMyBidResponse>>(null, model.pageNumber, model.pageSize);

                    var providerBidsList = await providerBids
                        .OrderByDescending(a => a.Bid.CreationDate)
                        .Skip((model.pageNumber - 1) * model.pageSize)
                        .Take(model.pageSize)
                        .AsSplitQuery()
                        .ToListAsync();

                    List<IndustryMiniModel> company_Industeries = new List<IndustryMiniModel>();
                    company_Industeries = await _companyIndustryRepository.Find(x => x.CompanyId == currentCompany.Id).
                        Select(y => new IndustryMiniModel { Id = y.CommercialSectorsTreeId, ParentId = y.CommercialSectorsTree.ParentId }).ToListAsync();


                    var userFavorites = await _userFavBidList.Find(x => x.UserId == usr.Id && providerBidsList.Select(s => s.Id).Contains(x.BidId)).ToListAsync();
                    var userFavBids = userFavorites.ToDictionary(x => x.BidId);


                    foreach (var providerBid in providerBidsList)
                    {
                        GetMyBidResponse response = new()
                        {
                            IsBuyCompetitionDocuments = true,
                            IsFunded = providerBid.Bid.IsFunded,
                            Regions = BidRegion.getAllRegionsAsListOfIds(providerBid.Bid.BidRegions)
                        };
                        await GetModelItemForBid(currentCompanyUser.Id, providerBid.Bid, response, providerBid.Bid.Association, providerBid.Bid.Donor, userFavorites);

                        response.hasProviderMatchIndustries = response.BidMainClassificationId.Any(x => company_Industeries.Any(y => y.ParentId == x.ParentId));
                        if (userFavBids.ContainsKey(providerBid.Id))
                            response.IsUserFavorite = true;
                        else
                            response.IsUserFavorite = false;
                        models.Add(response);
                    }

                    return new PagedResponse<List<GetMyBidResponse>>(models, model.pageNumber, model.pageSize, totalRecords);
                }
                else if (usr.UserType == UserType.Freelancer && usr.OrgnizationType == (int)OrganizationType.Freelancer)
                {
                    IQueryable<ProviderBid> providerBids = null;
                    Freelancer currentFreelancer = null;
                    ApplicationUser currentFreelancerUser = null;

                    currentFreelancer = await _freelancerRepository.FindOneAsync(x =>  x.IsVerified == true && x.Id == usr.CurrentOrgnizationId);
                    if (currentFreelancer is null)
                        return new PagedResponse<List<GetMyBidResponse>>
                            (HttpErrorCode.NotAuthorized, CommonErrorCodes.COMPANY_NOT_FOUND);

                    currentFreelancerUser = await _userManager.FindByEmailAsyncSafe(currentFreelancer.Email);
                    if (currentFreelancerUser == null)
                        return new PagedResponse<List<GetMyBidResponse>>
                            (HttpErrorCode.NotAuthorized, CommonErrorCodes.USER_NOT_EXIST);

                    providerBids = _providerBidRepository
                        .Find(x => x.FreelancerId == currentFreelancer.Id && x.IsPaymentConfirmed && !x.Bid.IsDeleted, false);



                    providerBids = providerBids
                        .Include(p => p.Bid)
                            .ThenInclude(b => b.BidAddressesTime)
                        .Include(b => b.Bid.Association)
                        .Include(b => b.Bid.BidRegions)
                            .ThenInclude(c => c.Region)
                        .Include(b => b.Bid.BidStatus)
                        .Include(b => b.Bid.BidType)
                        .Include(b => b.Bid.Bid_Industries).ThenInclude(y => y.CommercialSectorsTree).ThenInclude(z => z.Parent)
                        .Include(b => b.Bid.FreelanceBidIndustries).ThenInclude(y => y.FreelanceWorkingSector).ThenInclude(z => z.Parent)
                        .Include(b => b.Bid.BidType)
                        .Include(x => x.Bid.Donor)
                        .AsSplitQuery();

                    providerBids = ApplyFiltrationForProviderBids(model, providerBids,usr.UserType);

                    int totalRecords = await providerBids.CountAsync();
                    if (providerBids == null || totalRecords == 0)
                        return new PagedResponse<List<GetMyBidResponse>>(null, model.pageNumber, model.pageSize);

                    var providerBidsList = await providerBids
                        .OrderByDescending(a => a.Bid.CreationDate)
                        .Skip((model.pageNumber - 1) * model.pageSize)
                        .Take(model.pageSize)
                        .AsSplitQuery()
                        .ToListAsync();

                    List<IndustryMiniModel> freelancer_Industeries = new List<IndustryMiniModel>();
                    freelancer_Industeries = await _freelancerFreelanceWorkingSectorRepository.Find(x => x.FreelancerId == currentFreelancer.Id).
                        Select(y => new IndustryMiniModel { Id = y.FreelanceWorkingSectorId, ParentId = y.FreelanceWorkingSector.ParentId }).ToListAsync();


                    var userFavorites = await _userFavBidList.Find(x => x.UserId == usr.Id && providerBidsList.Select(s => s.Id).Contains(x.BidId)).ToListAsync();
                    var userFavBids = userFavorites.ToDictionary(x => x.BidId);


                    foreach (var providerBid in providerBidsList)
                    {
                        GetMyBidResponse response = new()
                        {
                            IsBuyCompetitionDocuments = true,
                            IsFunded = providerBid.Bid.IsFunded,
                            Regions = BidRegion.getAllRegionsAsListOfIds(providerBid.Bid.BidRegions)
                        };
                        await GetModelItemForBid(currentFreelancerUser.Id, providerBid.Bid, response, providerBid.Bid.Association, providerBid.Bid.Donor, userFavorites);

                        response.hasProviderMatchIndustries = response.BidMainClassificationId.Any(x => freelancer_Industeries.Any(y => y.ParentId == x.ParentId));
                        if (userFavBids.ContainsKey(providerBid.Id))
                            response.IsUserFavorite = true;
                        else
                            response.IsUserFavorite = false;
                        models.Add(response);
                    }

                    return new PagedResponse<List<GetMyBidResponse>>(models, model.pageNumber, model.pageSize, totalRecords);
                }
                else if (usr.UserType == UserType.Association || usr.UserType == UserType.Donor)
                {
                    IQueryable<Bid> bids = null;
                    ApplicationUser currentEntityUser = null;

                    Association association = null;
                    if (usr.UserType == UserType.Association)
                    {
                        association = await _associationService.GetUserAssociation(usr.Email);
                        if (association is null)
                            return new PagedResponse<List<GetMyBidResponse>>
                            (HttpErrorCode.NotFound, CommonErrorCodes.ASSOCIATION_NOT_FOUND);

                        currentEntityUser = await _userManager.FindByEmailAsyncSafe(association.Manager_Email);
                        if (currentEntityUser == null)
                            return new PagedResponse<List<GetMyBidResponse>>
                                (HttpErrorCode.NotAuthorized, CommonErrorCodes.USER_NOT_EXIST);
                    }
                    Donor donor = null;
                    if (usr.UserType == UserType.Donor)
                    {
                        donor = await _donorService.GetUserDonor(usr.Email);
                        if (donor is null)
                            return new PagedResponse<List<GetMyBidResponse>>
                            (HttpErrorCode.NotFound, CommonErrorCodes.DONOR_NOT_FOUND);

                        currentEntityUser = await _userManager.FindByEmailAsyncSafe(donor.ManagerEmail);
                        if (currentEntityUser == null)
                            return new PagedResponse<List<GetMyBidResponse>>
                                (HttpErrorCode.NotAuthorized, CommonErrorCodes.USER_NOT_EXIST);
                    }


                    bids = GetBidsForCurrentUser(usr, donor);
                    var IsUserHasCreateBidClaim = await _helperService.CheckIfUserHasSpecificClaims(new List<string> { Constants.AssociationCreateBidClaim, Constants.DonorCreateBidClaim }, usr);
                    bids = IsUserHasCreateBidClaim ? bids : bids.Where(x => x.BidStatusId != (int)TenderStatus.Draft);
                    bids = await ApplyFiltrationForBids(model, bids);

                    int totalRecords = await bids.CountAsync();
                    if (totalRecords == 0)
                        return new PagedResponse<List<GetMyBidResponse>>(null, model.pageNumber, model.pageSize);

                    var bidsList = await bids
                        .OrderByDescending(a => a.CreationDate)
                        .Skip((model.pageNumber - 1) * model.pageSize)
                        .Take(model.pageSize)
                        .AsSplitQuery()
                        .ToListAsync();

                    //var donorsId = bidsList.Where(x => x.EntityType == UserType.Donor).
                    //   Select(x => x.EntityId);
                    //var donors = await _donorRepository.FindAsync(don => donorsId.Contains(don.Id));
                    var userFavorites = await _userFavBidList.Find(x => x.UserId == usr.Id && bidsList.Select(s => s.Id).Contains(x.BidId)).ToListAsync();
                    var userFavBids = userFavorites.ToDictionary(x => x.BidId);

                    foreach (var bid in bidsList)
                    {
                        GetMyBidResponse response = new GetMyBidResponse
                        {
                            Regions = BidRegion.getAllRegionsAsListOfIds(bid.BidRegions),

                            IsOwner = (donor != null && (bid.DonorId == donor.Id)) || (association != null && (bid.AssociationId == association.Id))
                        };
                        await GetModelItemForBid(currentEntityUser.Id, bid, response, bid.Association, bid.Donor, userFavorites);

                        response.IsFunded = bid.IsFunded;

                        response.DonorName = bid.BidDonorId.HasValue ? bid.BidDonor.DonorId.HasValue ? bid.BidDonor.Donor.DonorName
                                              : bid.BidDonor.NewDonorName : "";

                        if (userFavBids.ContainsKey(bid.Id))
                            response.IsUserFavorite = true;
                        else
                            response.IsUserFavorite = false;

                        models.Add(response);
                    }
                    return new PagedResponse<List<GetMyBidResponse>>(models, model.pageNumber, model.pageSize, totalRecords);
                }
                return new PagedResponse<List<GetMyBidResponse>>(null, model.pageNumber, model.pageSize);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = $"pageSize = {model.pageSize} & pageNumber = {model.pageNumber}",
                    ErrorMessage = "Failed to Get Provider Payment Bids!",
                    ControllerAndAction = "BidController/my-bids"
                });
                return new PagedResponse<List<GetMyBidResponse>>(
                       HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }

        private IQueryable<Bid> GetBidsForCurrentUser(ApplicationUser usr, Donor donor)
        {
            IQueryable<Bid> bids = _bidRepository.Find(bid => !bid.IsDeleted, false)
                                        .Include(b => b.BidAddressesTime)
                                        .Include(b => b.Association)
                                        .Include(b => b.BidStatus)
                                        .Include(b => b.BidRegions)
                                            .ThenInclude(b => b.Region)
                                        .Include(b => b.Bid_Industries).ThenInclude(y => y.CommercialSectorsTree).ThenInclude(z => z.Parent)
                                        .Include(b => b.FreelanceBidIndustries).ThenInclude(y => y.FreelanceWorkingSector).ThenInclude(z => z.Parent)
                                        .Include(b => b.BidType)
                                        .Include(b => b.Donor)
                                        .Include(b => b.BidDonor.Donor)
                                        .Include(x => x.BidSupervisingData)
                                        .AsSplitQuery();

            bids = bids.Where(bid =>
            // owner of bid
            bid.EntityId == usr.CurrentOrgnizationId && bid.EntityType == usr.UserType
            // user is association and he is Supervising this bid
            || (bid.SupervisingAssociationId == usr.CurrentOrgnizationId && usr.UserType == UserType.Association)
            //user is donor and he is يمنح this bid
            || (bid.BidStatusId != (int)TenderStatus.Draft && donor != null && bid.BidDonorId.HasValue && bid.BidDonor.DonorId == donor.Id)
            // user id donor and he has Supervising claims at this bid
            || (bid.BidStatusId != (int)TenderStatus.Draft && donor != null &&
            (bid.BidSupervisingData.Where(x => x.SupervisorStatus != SponsorSupervisingStatus.Rejected).GroupBy(x => x.SupervisingServiceClaimCode).Select(x => x.OrderByDescending(d => d.Id).First().DonorId).Contains(donor.Id)))
            );
            return bids;
        }

        private static IQueryable<ProviderBid> ApplyFiltrationForProviderBids(FilterBidsSearchModel model, IQueryable<ProviderBid> providerBids, UserType userType)
        {
            if (model.IsBidAssignedForAssociationsOnly)
                providerBids = providerBids.Where(b => b.Bid.IsBidAssignedForAssociationsOnly);

            if (!string.IsNullOrEmpty(model.BiddingName))
                providerBids = providerBids.Where(b => b.Bid.BidName.Contains(model.BiddingName));

            if (!string.IsNullOrEmpty(model.AssociationName))
                providerBids = providerBids.Where(b => b.Bid.Association.Association_Name.Contains(model.AssociationName));

            providerBids = providerBids.WhereIf(!string.IsNullOrEmpty(model.SearchQuery),
                 b => DbFunctions.NormalizeArabic(b.Bid.Donor.DonorName).Contains(DbFunctions.NormalizeArabic(model.SearchQuery)) ||
                    DbFunctions.NormalizeArabic(b.Bid.Association.Association_Name).Contains(DbFunctions.NormalizeArabic(model.SearchQuery)) ||
                    DbFunctions.NormalizeArabic(b.Bid.BidName).Contains(DbFunctions.NormalizeArabic(model.SearchQuery)) ||
                    DbFunctions.NormalizeArabic(b.Bid.Ref_Number).Contains(DbFunctions.NormalizeArabic(model.SearchQuery)));
            if (!string.IsNullOrEmpty(model.BidTypeId))
            {
                var typeArrayAsString = model.BidTypeId.Split(',');
                int[] typesArray = Array.ConvertAll(typeArrayAsString, int.Parse);
                providerBids = providerBids.Where(b => typesArray.Contains((int)b.Bid.BidTypeId));
            }

            if (!string.IsNullOrEmpty(model.BidStatusId))
            {
                var statusArrayAsString = model.BidStatusId.Split(',');
                int[] statusArray = Array.ConvertAll(statusArrayAsString, int.Parse);
                providerBids = providerBids.Where(b => statusArray.Contains((int)b.Bid.BidStatusId));
            }

            if (!string.IsNullOrEmpty(model.RegionId))
            {
                var regionsIdAsString = model.RegionId.Split(',');
                int[] regionsId = Array.ConvertAll(regionsIdAsString, int.Parse);
                providerBids = providerBids.Where(b => b.Bid.BidRegions.Any(a => regionsId.Contains(a.RegionId)));
            }

            if (!string.IsNullOrEmpty(model.BidMainClassificationId))
            {
                var statusArrayAsString = model.BidMainClassificationId.Split(',');
                int[] statusArray = Array.ConvertAll(statusArrayAsString, int.Parse);
                providerBids = providerBids.Where(b => b.Bid.Bid_Industries.Any(a => statusArray.Contains((int)a.CommercialSectorsTreeId)));
            }
            if (!string.IsNullOrEmpty(model.FreelancingBidMainClassificationId))
            {
                var statusArrayAsString = model.FreelancingBidMainClassificationId.Split(',');
                int[] statusArray = Array.ConvertAll(statusArrayAsString, int.Parse);
                providerBids = providerBids.Where(b => b.Bid.FreelanceBidIndustries.Any(a => statusArray.Contains((int)a.FreelanceWorkingSectorId))) ;
            }
          

            if (!string.IsNullOrEmpty(model.BiddingRefNumber))
                providerBids = providerBids.Where(b => b.Bid.Ref_Number.Contains(model.BiddingRefNumber));

            if (model.PublishDateFrom != null && model.PublishDateFrom != default)
                providerBids = providerBids.Where(b => DateTime.Compare(b.Bid.CreationDate.Date, model.PublishDateFrom.Value.Date) >= 0);

            if (model.PublishDateTo != null && model.PublishDateTo != default)
                providerBids = providerBids.Where(b => DateTime.Compare(b.Bid.CreationDate.Date, model.PublishDateTo.Value.Date) <= 0);

            if (model.OfferSubmissionDateFrom != null && model.OfferSubmissionDateFrom != default)
                providerBids = providerBids.Where(b => DateTime.Compare(((DateTime)b.Bid.BidAddressesTime.LastDateInOffersSubmission).Date, model.OfferSubmissionDateFrom.Value.Date) >= 0);

            if (model.OfferSubmissionDateTo != null && model.OfferSubmissionDateTo != default)
                providerBids = providerBids.Where(b => DateTime.Compare(((DateTime)b.Bid.BidAddressesTime.LastDateInOffersSubmission).Date, model.OfferSubmissionDateTo.Value.Date) <= 0);


            if (model.TermsBookPriceId != null)
            {
                switch (model.TermsBookPriceId)
                {
                    case (int)TermsBookPrice.Free:
                        providerBids = providerBids.Where(a => a.Price == 0);
                        break;
                    case (int)TermsBookPrice.p1_1000:
                        providerBids = providerBids.Where(a => a.Price >= 1 && a.Price <= 1000);
                        break;
                    case (int)TermsBookPrice.p1001_10000:
                        providerBids = providerBids.Where(a => a.Price >= 1001 && a.Price <= 10000);
                        break;
                    case (int)TermsBookPrice.p10001_20000:
                        providerBids = providerBids.Where(a => a.Price >= 10001 && a.Price <= 20000);
                        break;
                    case (int)TermsBookPrice.p20001_40000:
                        providerBids = providerBids.Where(a => a.Price >= 20001 && a.Price <= 40000);
                        break;
                    case (int)TermsBookPrice.p40001_50000:
                        providerBids = providerBids.Where(a => a.Price >= 40001 && a.Price <= 50000);
                        break;
                    case (int)TermsBookPrice.greater50000:
                        providerBids = providerBids.Where(a => a.Price > 50000);
                        break;

                    default:
                        break;
                }
            }
            if (model.CancelDateFrom != null && model.CancelDateFrom != default)
                providerBids = providerBids.Where(b => b.Bid.CancelDate.Value.Date >= model.CancelDateFrom.Value.Date);

            if (model.CancelDateTo != null && model.CancelDateTo != default)
                providerBids = providerBids.Where(b => b.Bid.CancelDate.Value.Date <= model.CancelDateTo.Value.Date);


            if (model.ActualAnchoringDateFrom != null && model.ActualAnchoringDateFrom != default)
                providerBids = providerBids.Where(b => b.Bid.ActualAnchoringDate.Value.Date >= model.ActualAnchoringDateFrom.Value.Date);

            if (model.ActualAnchoringDateTo != null && model.ActualAnchoringDateTo != default)
                providerBids = providerBids.Where(b => b.Bid.ActualAnchoringDate.Value.Date <= model.ActualAnchoringDateTo.Value.Date);
            return providerBids;
        }

        private async Task<IQueryable<Bid>> ApplyFiltrationForBids(FilterBidsSearchModel model, IQueryable<Bid> bids,bool getFreelancingBids=false)
        {
            if (model.IsBidAssignedForAssociationsOnly)
                bids = bids.Where(b => b.IsBidAssignedForAssociationsOnly);

            if (!string.IsNullOrEmpty(model.BiddingName))
                bids = bids.Where(b => b.BidName.Contains(model.BiddingName));

            bids = bids.WhereIf(!string.IsNullOrEmpty(model.SearchQuery),
                b => DbFunctions.NormalizeArabic(b.Donor.DonorName).Contains(DbFunctions.NormalizeArabic(model.SearchQuery)) ||
                   DbFunctions.NormalizeArabic(b.Association.Association_Name).Contains(DbFunctions.NormalizeArabic(model.SearchQuery)) ||
                   DbFunctions.NormalizeArabic(b.BidName).Contains(DbFunctions.NormalizeArabic(model.SearchQuery)) ||
                   DbFunctions.NormalizeArabic(b.Ref_Number).Contains(DbFunctions.NormalizeArabic(model.SearchQuery)));

            if (!string.IsNullOrEmpty(model.AssociationName))
            {
                var donorsWithSameName = await _donorRepository.Find(a => a.DonorName.Contains(model.AssociationName)).Select(a => a.Id).ToListAsync();

                bids = bids.Where(b => (b.EntityType == UserType.Association && b.Association.Association_Name.Contains(model.AssociationName))
                                    || (b.EntityType == UserType.Donor && donorsWithSameName.Contains(b.EntityId)));
            }
            if (!string.IsNullOrEmpty(model.BidTypeId))
            {
                var typeArrayAsString = model.BidTypeId.Split(',');
                int[] typesArray = Array.ConvertAll(typeArrayAsString, int.Parse);
                bids = bids.Where(b => typesArray.Contains((int)b.BidTypeId));
            }

            if (!string.IsNullOrEmpty(model.BidStatusId))
            {
                var statusArrayAsString = model.BidStatusId.Split(',');
                int[] statusArray = Array.ConvertAll(statusArrayAsString, int.Parse);
                bids = bids.Where(b => statusArray.Contains((int)b.BidStatusId));
            }

            if (!string.IsNullOrEmpty(model.RegionId))
            {
                var regionsIdAsString = model.RegionId.Split(',');
                int[] regionsId = Array.ConvertAll(regionsIdAsString, int.Parse);
                bids = bids.Where(b => b.BidRegions.Any(a => regionsId.Contains(a.RegionId)));
            }

            if ((!string.IsNullOrEmpty(model.BidMainClassificationId) && !getFreelancingBids) || !string.IsNullOrEmpty(model.FreelancingBidMainClassificationId))
            {
                if ((!string.IsNullOrEmpty(model.BidMainClassificationId) && !getFreelancingBids) && string.IsNullOrEmpty(model.FreelancingBidMainClassificationId))
                {
                    var statusArrayAsString = model.BidMainClassificationId.Split(',');
                    int[] statusArray = Array.ConvertAll(statusArrayAsString, int.Parse);

                    bids = bids.Where(b => b.Bid_Industries.Any(a => statusArray.Contains((int)a.CommercialSectorsTreeId)));
                }

                else if ((string.IsNullOrEmpty(model.BidMainClassificationId)) && !string.IsNullOrEmpty(model.FreelancingBidMainClassificationId))
                {
                    var statusArrayAsString = model.FreelancingBidMainClassificationId.Split(',');
                    int[] statusArray = Array.ConvertAll(statusArrayAsString, int.Parse);

                    bids = bids.Where(b => b.FreelanceBidIndustries.Any(a => statusArray.Contains((int)a.FreelanceWorkingSectorId)));
                }

                if ((!string.IsNullOrEmpty(model.BidMainClassificationId) && !getFreelancingBids) && !string.IsNullOrEmpty(model.FreelancingBidMainClassificationId))
                {
                    var statusArrayAsString = model.BidMainClassificationId.Split(',');
                    int[] statusArray = Array.ConvertAll(statusArrayAsString, int.Parse);

                    List<int> bidMainClassificationIds = model.BidMainClassificationId.Split(',').Select(int.Parse).ToList();

                    List<int> freelancingBidMainClassificationIds = model.FreelancingBidMainClassificationId.Split(',').Select(int.Parse).ToList();


                    bids = bids.Where(b =>(bidMainClassificationIds.Any() && b.Bid_Industries.Any(a => bidMainClassificationIds.Contains((int)a.CommercialSectorsTreeId)))
                                       || (freelancingBidMainClassificationIds.Any() && b.FreelanceBidIndustries.Any(a => freelancingBidMainClassificationIds.Contains((int)a.FreelanceWorkingSectorId))) );
                }
            }

            if (!string.IsNullOrEmpty(model.BiddingRefNumber))
                bids = bids.Where(b => b.Ref_Number.Contains(model.BiddingRefNumber));

            if (model.PublishDateFrom != null && model.PublishDateFrom != default)
                bids = bids.Where(b => DateTime.Compare(b.CreationDate.Date, model.PublishDateFrom.Value.Date) >= 0);

            if (model.PublishDateTo != null && model.PublishDateTo != default)
                bids = bids.Where(b => DateTime.Compare(b.CreationDate.Date, model.PublishDateTo.Value.Date) <= 0);

            if (model.OfferSubmissionDateFrom != null && model.OfferSubmissionDateFrom != default)
                bids = bids.Where(b => DateTime.Compare(((DateTime)b.BidAddressesTime.LastDateInOffersSubmission).Date, model.OfferSubmissionDateFrom.Value.Date) >= 0);

            if (model.OfferSubmissionDateTo != null && model.OfferSubmissionDateTo != default)
                bids = bids.Where(b => DateTime.Compare(((DateTime)b.BidAddressesTime.LastDateInOffersSubmission).Date, model.OfferSubmissionDateTo.Value.Date) <= 0);

            //if (model.TermsBookPriceId != null)
            //{
            //    switch (model.TermsBookPriceId)
            //    {
            //        case (int)TermsBookPrice.Free:
            //            bids = bids.Where(a => a.Bid_Documents_Price == 0);
            //            break;
            //        case (int)TermsBookPrice.p1_1000:
            //            bids = bids.Where(a => a.Bid_Documents_Price >= 1 && a.Bid_Documents_Price <= 1000);
            //            break;
            //        case (int)TermsBookPrice.p1001_10000:
            //            bids = bids.Where(a => a.Bid_Documents_Price >= 1001 && a.Bid_Documents_Price <= 10000);
            //            break;
            //        case (int)TermsBookPrice.p10001_20000:
            //            bids = bids.Where(a => a.Bid_Documents_Price >= 10001 && a.Bid_Documents_Price <= 20000);
            //            break;
            //        case (int)TermsBookPrice.p20001_40000:
            //            bids = bids.Where(a => a.Bid_Documents_Price >= 20001 && a.Bid_Documents_Price <= 40000);
            //            break;
            //        case (int)TermsBookPrice.p40001_50000:
            //            bids = bids.Where(a => a.Bid_Documents_Price >= 40001 && a.Bid_Documents_Price <= 50000);
            //            break;
            //        case (int)TermsBookPrice.greater50000:
            //            bids = bids.Where(a => a.Bid_Documents_Price > 50000);
            //            break;
            //        default:
            //            break;
            //    }
            //}
           
            bids = bids.WhereIf(model.CreatorType != null, b => b.EntityType == model.CreatorType.Value);


            if (model.CancelDateFrom != null && model.CancelDateFrom != default)
                bids = bids.Where(b => b.CancelDate.Value.Date >= model.CancelDateFrom.Value.Date);

            if (model.CancelDateTo != null && model.CancelDateTo != default)
                bids = bids.Where(b => b.CancelDate.Value.Date <= model.CancelDateTo.Value.Date);


            if (model.ActualAnchoringDateFrom != null && model.ActualAnchoringDateFrom != default)
                bids = bids.Where(b => b.ActualAnchoringDate.Value.Date >= model.ActualAnchoringDateFrom.Value.Date);

            if (model.ActualAnchoringDateTo != null && model.ActualAnchoringDateTo != default)
                bids = bids.Where(b => b.ActualAnchoringDate.Value.Date <= model.ActualAnchoringDateTo.Value.Date);

            var termsBookPriceArray = !string.IsNullOrWhiteSpace(model.TermsBookPrice) && model.TermsBookPrice.Contains(',')
                                      ? model.TermsBookPrice.Split(',')
                                           .Select(s => s.Trim())
                                           .Where(s => int.TryParse(s, out _))
                                           .Select(int.Parse)
                                           .ToList()
                                      : new List<int>();

            if (termsBookPriceArray.Count > 1)
            {
                bids = bids.Where(a => a.Bid_Documents_Price >= termsBookPriceArray[0] &&
                                      a.Bid_Documents_Price <= termsBookPriceArray[1]);
            }

            return bids;
        }


        private async Task GetModelItemForBid(string currentUserId, Bid bid, GetMyBidResponse model, Association associationBid, Donor donor, List<UserFavBidList> userFavorites)
        {
            model.Id = bid.Id;
            model.Title = bid.BidName;
            model.EntityType = bid.AssociationId.HasValue ? UserType.Association : UserType.Donor;
            model.EntityId = bid.AssociationId.HasValue ? bid.AssociationId.Value : bid.DonorId.Value;
            model.Price = bid.Bid_Documents_Price;
            model.Ref_Number = bid.Ref_Number;
            model.BidStatusId = bid.BidStatusId;
            model.CreationDate = bid.CreationDate;
            model.LastDateInOffersSubmission = bid.BidAddressesTime != null ? bid.BidAddressesTime.LastDateInOffersSubmission : null;
            model.IsUserFavorite = IsBidFavoriteByCurrentUser(currentUserId, bid.Id, userFavorites);
            model.BidTypeId = bid.BidTypeId;
            model.BidTypeName = bid.BidTypeId.HasValue ? bid.BidType.NameAr : "";
            model.BidVisibility = (int)bid.BidVisibility;
            model.DeliveryPlace = bid.DeliveryPlace;
            model.IsBidAssignedForAssociationsOnly = bid.IsBidAssignedForAssociationsOnly;

            model.RegionsNames = bid.BidRegions.Select(b => b.Region.NameAr).ToList();
            var workingSectors = bid.GetBidWorkingSectors();
            model.BidMainClassificationId = workingSectors.
               Select(a => new BidMainClassificationIds { Id = a.Id, ParentId = a.ParentId }).ToList().DistinctBy(x => x.Id).ToList();
            model.BidMainClassificationNames = workingSectors
                .Select(i => new BidMainClassificationNames { Name = i.NameAr, ParentName = i.Parent?.NameAr }).DistinctBy(x => x.Name).ToList();



            if (bid.EntityType == UserType.Association)
            {

                model.EntityLogoResponse = await _imageService.GetFileResponseEncrypted(associationBid.Image, associationBid.ImageFileName);
                model.EntityName = associationBid.Association_Name;
                model.EntityImage = associationBid.Image;
                model.EntityImageFileName = associationBid.ImageFileName;
            }
            else if (bid.EntityType == UserType.Donor)
            {

                model.EntityLogoResponse = await _imageService.GetFileResponseEncrypted(bid.Donor.Image, bid.Donor.ImageFileName);
                model.EntityName = donor.DonorName;
                model.EntityImage = donor.Image;
                model.EntityImageFileName = donor.ImageFileName;
            }
        }

        private bool IsBidFavoriteByCurrentUser(string userId, long bidId, List<UserFavBidList> userFavorites)
        {
            if (string.IsNullOrEmpty(userId))
                return false;

            var result = userFavorites.Where(x => x.UserId == userId && x.BidId == bidId).FirstOrDefault();
            return result == null ? false : true;
        }

        public async Task<PagedResponse<IReadOnlyList<GetProvidersBidsReadOnly>>> GetpProviderBids(int pageSize = 10, int pageNumber = 1)
        {
            try
            {
                var usr = _currentUserService.CurrentUser;
                if (usr == null || usr.UserType == UserType.Association)
                {
                    return new PagedResponse<IReadOnlyList<GetProvidersBidsReadOnly>>(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);
                }
                IQueryable<ProviderBid> providerBids = null;
                //providerBids = await _providerBidRepository.Find(x => x.Company.Email == usr.Email, false,
                //   nameof(ProviderBid.Bid)).ToListAsync();

                if (usr.UserType == UserType.Provider)
                {
                    if (usr.OrgnizationType == (int)OrganizationType.Comapny)
                    {
                        var userCompany = await _companyRepository.FindOneAsync(x => x.Id == usr.CurrentOrgnizationId, false);
                        if (userCompany != null)
                        {
                            providerBids = _providerBidRepository.Find(x => x.CompanyId == userCompany.Id && x.IsPaymentConfirmed, false,
                        nameof(ProviderBid.Bid), nameof(ProviderBid.Company));
                        }
                    }
                    //var provider = await _providerRepository.FindOneAsync(x => x.Email.ToLower() == usr.Email.ToLower(), false);
                    //var userCompany = await _companyService.GetProviderCompanies(provider.Id);
                    //foreach (var item in userCompany)
                    //{
                    //    providerBids.AddRange(_providerBidRepository.Find(x => x.CompanyId == item.Id, false).ToList());
                    //}
                }
                else if (usr.UserType == UserType.Company)
                {
                    var userCompany = await _companyService.GetUserCompany(usr.Email);
                    if (userCompany != null)
                    {
                        providerBids = _providerBidRepository.Find(x => x.CompanyId == userCompany.Id && x.IsPaymentConfirmed, false,
                     nameof(ProviderBid.Bid), nameof(ProviderBid.Company));
                    }
                }
                providerBids = providerBids.Include(x => x.Bid).ThenInclude(x => x.Association);
                providerBids = providerBids.Include(x => x.Bid).ThenInclude(x => x.BidMainClassificationMapping);
                providerBids = providerBids.Include(x => x.Bid).ThenInclude(x => x.BidAddressesTime);
                providerBids = providerBids.Include(x => x.Bid).ThenInclude(x => x.BidStatus);
                providerBids = providerBids.Include(x => x.Bid).ThenInclude(x => x.BidType);
                if (providerBids.Count() <= 0 || providerBids == null)
                {

                    //return new PagedResponse<IReadOnlyList<GetProvidersBidsReadOnly>>(
                    //       HttpErrorCode.NotFound, CommonErrorCodes.NOT_FOUND);          
                    return new PagedResponse<IReadOnlyList<GetProvidersBidsReadOnly>>(
                           new List<GetProvidersBidsReadOnly>() as IReadOnlyList<GetProvidersBidsReadOnly>, pageNumber, pageSize, 0);

                }
                int totalRecords = providerBids.Count();
                //providerBids = providerBids.OrderByDescending(a => a.Bid.CreationDate).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
                providerBids = providerBids.OrderByDescending(a => a.CreationDate).Skip((pageNumber - 1) * pageSize).Take(pageSize);

                List<GetProvidersBidsReadOnly> models = new List<GetProvidersBidsReadOnly>();
                foreach (var bid in providerBids)
                {

                    var bidAddressesTime = await _bidAddressesTimeRepository.FindOneAsync(x => x.BidId == bid.BidId);
                    int days = 0;
                    int hour = 0;
                    if (bidAddressesTime != null && bidAddressesTime.LastDateInOffersSubmission > _dateTimeZone.CurrentDate)
                    {
                        var countdwnon = ((DateTime)bidAddressesTime.LastDateInOffersSubmission).Subtract(_dateTimeZone.CurrentDate).TotalHours;
                        days = (int)(countdwnon / 24);

                        hour = (int)(countdwnon % 24);

                    }
                    var association = await _bidRepository.FindOneAsync(x => x.Id == bid.BidId, false, nameof(Bid.Association));

                    var paymentBasicData = new PaymentTransactionBasicData { TransactionNumber = bid.TransactionNumber, PaymentMethod = (Nafes.CrossCutting.Model.Enums.PaymentMethod)bid.PaymentMethodId };
                    var payments = await _helperService.GetPaymentTransaction(new List<PaymentTransactionBasicData> { paymentBasicData });
                    var transactionNumber = payments.FirstOrDefault();

                    GetProvidersBidsReadOnly model = new GetProvidersBidsReadOnly
                    {
                        AssociationLogoResponse = await _imageService.GetFileResponseEncrypted(association.Association.Image, association.Association.ImageFileName),
                        CompanyLogoResponse = await _imageService.GetFileResponseEncrypted(bid.Company.Image, bid.Company.ImageFileName),
                        AssociationName = association.Association.Association_Name,
                        TransactionNumber = transactionNumber == null ? "" : transactionNumber.TranRef,
                        CreationDate = bid.CreationDate,
                        Price = bid.Price,
                        Id = bid.BidId,
                        Title = bid.Bid.BidName,
                        CountdownToCompleteDays = days,
                        CountdownToCompleteHours = hour,
                        BidModel = _mapper.Map<ReadOnlyFilterBidModel>(bid.Bid)
                    };

                    models.Add(model);
                }

                var bidsModels = _mapper.Map<IReadOnlyList<GetProvidersBidsReadOnly>>(models.ToList());

                //bidsModels.ToList().ForEach( async x => x.AssociationLogo = !string.IsNullOrEmpty(x.AssociationLogo) ? fileSettings.BASE_URL + x.AssociationLogo : x.AssociationLogo);
                //bidsModels.ToList().ForEach(x => x.CompanyLogo = !string.IsNullOrEmpty(x.CompanyLogo) ? fileSettings.BASE_URL + x.CompanyLogo : x.CompanyLogo);

                return new PagedResponse<IReadOnlyList<GetProvidersBidsReadOnly>>(bidsModels, pageNumber, pageSize, totalRecords);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = $"pageSize = {pageSize} & pageNumber = {pageNumber}",
                    ErrorMessage = "Failed to Get Provider Payment Bids!",
                    ControllerAndAction = "ProviderController/GetProviderPaymentBids"
                });
                return new PagedResponse<IReadOnlyList<GetProvidersBidsReadOnly>>(
                       HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }

        public async Task<PagedResponse<IReadOnlyList<GetMyBidResponse>>> GetAllBids(FilterBidsSearchModel request)
        {
            try
            {
                if (request.PublishDateFrom > request.PublishDateTo)
                    return new PagedResponse<IReadOnlyList<GetMyBidResponse>>(HttpErrorCode.InvalidInput, BidErrorCodes.PUBLISH_DATE_FROM_MUST_BE_EQUAL_TO_OR_BEFORE_PUBLISH_DATE_TO);
                if (request.OfferSubmissionDateFrom > request.OfferSubmissionDateTo)
                    return new PagedResponse<IReadOnlyList<GetMyBidResponse>>(HttpErrorCode.InvalidInput, BidErrorCodes.OFFER_SUBMISSION_DATE_FROM_MUST_BE_EQUAL_TO_OR_BEFORE_OFFER_SUBMISSION_DATE_TO);

                var user = _currentUserService.CurrentUser;
                await _bidSearchLogService.AddBidSearcLogAsync(request, user);

                if (user is null)
                    return await GetAllBidsForVisitor(request);

                var administrationUserTypes = Constants.AdminstrationUserTypes;
                var isAdminHasBidClaim = user.UserType == UserType.Admin ? await _helperService.CheckIfUserHasSpecificClaims(new List<string>() { AdminClaimCodes.clm_2553.AsName() }, user) : false;

                IQueryable<Bid> bids = _bidRepository.Find(x => !x.IsDeleted, true, false)
                   .WhereIf(!isAdminHasBidClaim && user.UserType == UserType.Admin, x => x.BidStatusId != (int)TenderStatus.Draft)
                   .WhereIf(!administrationUserTypes.Contains(user.UserType), x => !x.IsBidHidden)
                   .WhereIf(user.UserType == UserType.Freelancer, x => (BidTypes)x.BidTypeId == BidTypes.Freelancing)
                   .WhereIf(user.UserType == UserType.Provider, x => (BidTypes)x.BidTypeId != BidTypes.Freelancing)
                   .WhereIf(user.UserType == UserType.SupportMember || user.UserType == UserType.SupportManager, b => b.BidStatusId != (int)TenderStatus.Draft)
                   .WhereIf(user.UserType == UserType.Supervisors, b => b.BidStatusId != (int)TenderStatus.Draft && b.BidStatusId != (int)TenderStatus.Reviewing && b.BidStatusId != (int)TenderStatus.Pending);

                if (user.UserType == UserType.Freelancer || (user.UserType == UserType.Provider && user.OrgnizationType == (int)OrganizationType.Comapny))
                    return await GetAllBidsForBidParticipants(request, bids,user);


                Association association = null;
                Donor donor = null;

                if (user.UserType == UserType.Association || user.UserType == UserType.Donor)
                {
                    if (user.UserType == UserType.Association)
                    {
                        association = await _associationService.GetUserAssociation(user.Email);
                        if (association is null)
                            return new PagedResponse<IReadOnlyList<GetMyBidResponse>>(HttpErrorCode.NotFound, CommonErrorCodes.ASSOCIATION_NOT_FOUND);

                        bids = bids.Union(GetPrivateBidsForCurrentAssociation(association.Id, user.CurrentOrgnizationId));
                    }

                    if (user.UserType == UserType.Donor)
                    {
                        donor = await _donorRepository.FindOneAsync(don => don.Id == user.CurrentOrgnizationId && don.isVerfied && !don.IsDeleted);
                        if (donor == null)
                            return new PagedResponse<IReadOnlyList<GetMyBidResponse>>(HttpErrorCode.NotFound, CommonErrorCodes.DONOR_NOT_FOUND);

                        bids = bids.Union(GetPrivateBidsForCurrentDonor(donor.Id, user.CurrentOrgnizationId)); 
                        bids = bids.Union(GetPrivateBidsForSupervisorDonor(donor.Id));
                    }

                    var othersDraftedBids = bids
                        .Where(bid => bid.BidStatusId == (int)TenderStatus.Draft && (bid.EntityId != user.CurrentOrgnizationId || bid.EntityType != user.UserType));
                    bids = bids.Where(b => !othersDraftedBids.Any(x => x.Id == b.Id));

                    var reviewingBidsOfCurrentEntity = bids.Where(x => x.BidStatusId == (int)TenderStatus.Reviewing && x.EntityId == user.CurrentOrgnizationId && x.EntityType == user.UserType);
                    bids = bids.Where(b => b.BidStatusId != (int)TenderStatus.Reviewing);
                    bids = bids.Union(reviewingBidsOfCurrentEntity);

                    var IsUserHasCreateBidClaim = await _helperService.CheckIfUserHasSpecificClaims(new List<string> { Constants.AssociationCreateBidClaim, Constants.DonorCreateBidClaim }, user);
                    bids = IsUserHasCreateBidClaim ? bids : bids.Where(x => x.BidStatusId != (int)TenderStatus.Draft);
                }


                bids = await ApplyFiltrationForBids(request, bids);
                var totalRecords = await bids.Distinct().CountAsync();
                if (totalRecords == 0)
                    return new PagedResponse<IReadOnlyList<GetMyBidResponse>>(null, request.pageNumber, request.pageSize, totalRecords);

                bids = bids
                    .Distinct()
                    .OrderByDescending(a => a.CreationDate)
                    .ApplyPaging(request.pageNumber, request.pageSize)
                    .IncludeBasicBidData()
                    .Include(a => a.BidType);

                if (user.UserType == UserType.Donor)
                    bids = bids.Include(b => b.BidSupervisingData.Where(s => s.SupervisorStatus != SponsorSupervisingStatus.Rejected));

                var result = await bids
                    .AsSplitQuery()
                    .ToListAsync();

                var bidsModels = _mapper.Map<List<GetMyBidResponse>>(result);

                await MapAllBidsResult(user, null, association, donor, null, null, result, bidsModels);
                return new PagedResponse<IReadOnlyList<GetMyBidResponse>>(bidsModels, request.pageNumber, request.pageSize, totalRecords);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = request,
                    ErrorMessage = "Failed to search bids!",
                    ControllerAndAction = "BidController/all-bids"
                });
                return new PagedResponse<IReadOnlyList<GetMyBidResponse>>(
                       HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }


        private async Task<PagedResponse<IReadOnlyList<GetMyBidResponse>>> GetAllBidsForVisitor(FilterBidsSearchModel request)
        {
            try
            {
                IQueryable<Bid> bids = _bidRepository
                    .Find(x => !x.IsDeleted && !x.IsBidHidden,true, false)
                    .Where(x => x.BidStatusId != (int)TenderStatus.Draft)
                    .Where(x => x.BidStatusId != (int)TenderStatus.Reviewing)
                    .Where(x => x.BidStatusId != (int)TenderStatus.Cancelled)
                    .Where(x => x.BidStatusId != (int)TenderStatus.Pending);

                bids = await ApplyFiltrationForBids(request, bids);

                var totalRecords = await bids.CountAsync();
                if (totalRecords == 0)
                    return new PagedResponse<IReadOnlyList<GetMyBidResponse>>(null, request.pageNumber, request.pageSize);

                var result = await bids
                    .Distinct()
                    .OrderByDescending(a => a.CreationDate)
                    .ApplyPaging(request.pageNumber, request.pageSize)
                    .IncludeBasicBidData()
                    .Include(a => a.BidType)
                    .AsSplitQuery()
                    .ToListAsync();

                var bidsModels = _mapper.Map<List<GetMyBidResponse>>(result);
                var bidsIds = bidsModels.Select(a => a.Id);
                await MapBidModelsBasicData(result, bidsModels, bidsIds);
                bidsModels.ForEach(model =>
                {
                    model.EntityName = null;
                    //model.EntityLogoResponse = null;
                });

                await MapAllBidsResultForVisitor(result, bidsModels);
                return new PagedResponse<IReadOnlyList<GetMyBidResponse>>(bidsModels, request.pageNumber, request.pageSize, totalRecords);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = request,
                    ErrorMessage = "Failed to search bids for visitor!",
                    ControllerAndAction = "BidController/all-bids"
                });
                return new PagedResponse<IReadOnlyList<GetMyBidResponse>>(
                       HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }
       
        private async Task<PagedResponse<IReadOnlyList<GetMyBidResponse>>> GetAllBidsForBidParticipants(FilterBidsSearchModel request, IQueryable<Bid> bids, ApplicationUser user)
        {
            Company company = null;
            Freelancer freelancer = null;
            List<IndustryMiniModel> bidParticipantWorkingSectorsMiniData = new();

            if (user.UserType == UserType.Provider)
            {
                company = await _companyRepository.Find(x => x.IsDeleted == false && x.isVerfied == true && x.Id == user.CurrentOrgnizationId, false, nameof(Company.Provider))
                    .Include(x => x.Company_Industries)
                        .ThenInclude(x => x.CommercialSectorsTree)
                    .FirstOrDefaultAsync();

                if (company is null)
                    return new PagedResponse<IReadOnlyList<GetMyBidResponse>>(HttpErrorCode.NotFound, CommonErrorCodes.COMPANY_NOT_FOUND);

                bidParticipantWorkingSectorsMiniData = company.Company_Industries.Where(x => x.CommercialSectorsTreeId.HasValue).Select(x => new IndustryMiniModel {Id = x.CommercialSectorsTreeId.Value, ParentId = x.CommercialSectorsTree.ParentId} ).ToList();
            }
            else if (user.UserType == UserType.Freelancer)
            {
                freelancer = await _freelancerRepository.Find(x => x.IsVerified && x.Id == user.CurrentOrgnizationId)
                    .Include(x => x.FreelancerWorkingSectors)
                        .ThenInclude(x => x.FreelanceWorkingSector)
                    .FirstOrDefaultAsync();

                if (freelancer is null)
                    return new PagedResponse<IReadOnlyList<GetMyBidResponse>>(HttpErrorCode.NotFound, CommonErrorCodes.FREELANCER_NOT_FOUND);

                bidParticipantWorkingSectorsMiniData = freelancer.FreelancerWorkingSectors.Select(x => new IndustryMiniModel {Id = x.FreelanceWorkingSectorId, ParentId = x.FreelanceWorkingSector.ParentId}).ToList();
            }
            else
                throw new ArgumentException($"This Enum {user.UserType.ToString()} not Handled Here {nameof(BidServiceCore.GetAllBidsForBidParticipants)}");

            if (user.UserType == UserType.Provider && user.OrgnizationType == (int)OrganizationType.Comapny && company.AssignedAssociationId is null && company.AssignedDonorId is null)
                bids = bids.Where(b => !b.IsBidAssignedForAssociationsOnly);

            // المنافسات المسندة التي تم شراء وثائقها من قبل
            if(user.UserType == UserType.Company)
                bids = bids.Union(GetAssignedForAssociationsOnlyBidsAndThisCompanyBoughtTermsBook(company.Id));//check performance of union 


            bids = bids.Where(x => (x.BidStatusId != (int)TenderStatus.Draft
                    && x.BidStatusId != (int)TenderStatus.Pending
                    && x.BidStatusId != (int)TenderStatus.Cancelled
                    && x.BidStatusId != (int)TenderStatus.Reviewing));

            bids = bids.Include(a => a.BidAddressesTime)
             .IncludeBasicBidData()
             .Include(x => x.Donor)
             .Include(a => a.BidType);

            bids = await ApplyFiltrationForBids(request, bids);
            bids = GetRelatedBidsToBidParticipantWorkingSectorsFirstOrder(bids, freelancer, company, user, bidParticipantWorkingSectorsMiniData);

            var totalRecordsForCompany = await bids.CountAsync();
            if (totalRecordsForCompany == 0)
                return new PagedResponse<IReadOnlyList<GetMyBidResponse>>(null, request.pageNumber, request.pageSize);

            var resultForCompany = await bids
                        .ApplyPaging(request.pageNumber, request.pageSize)
                        .ToListAsync();

            var bidsModelsForCompany = _mapper.Map<List<GetMyBidResponse>>(resultForCompany);

            await MapAllBidsResult(user, company, null, null, freelancer, bidParticipantWorkingSectorsMiniData, resultForCompany, bidsModelsForCompany);
            return new PagedResponse<IReadOnlyList<GetMyBidResponse>>(bidsModelsForCompany, request.pageNumber, request.pageSize, totalRecordsForCompany);
        }

        private IQueryable<Bid> GetAssignedForAssociationsOnlyBidsAndThisCompanyBoughtTermsBook(long companyId)
        {
            return _bidRepository.Find(x =>
                            !x.IsDeleted
                            && x.IsBidAssignedForAssociationsOnly
                            && x.ProviderBids.Any(p => p.IsPaymentConfirmed && p.CompanyId == companyId), true);
        }

        private IQueryable<ProviderBid> GetAssignedForAssociationsOnlyProviderBidsAndThisCompanyBoughtTermsBook(long companyId)
        {
            return _providerBidRepository.Find(x =>
                            !x.Bid.IsDeleted
                            && x.Bid.IsBidAssignedForAssociationsOnly
                            && x.IsPaymentConfirmed
                            && x.CompanyId == companyId, true);
        }

        private IQueryable<Bid> GetRelatedBidsToBidParticipantWorkingSectorsFirstOrder(IQueryable<Bid> bids, Freelancer freelancer, Company company, ApplicationUser user, List<IndustryMiniModel> bidParticipantWorkingSectorsMiniData)
        {
            var bidParticipantWorkingSectorsIds = bidParticipantWorkingSectorsMiniData.Select(x => x.Id);

            var bidsRelatedToCompaniesIndustries = bids
                .WhereIf(user.UserType == UserType.Provider, x => x.Bid_Industries.Any(bi => bidParticipantWorkingSectorsIds.Contains(bi.CommercialSectorsTreeId.Value)) && x.BidStatusId == (int)TenderStatus.Open)
                .WhereIf(user.UserType == UserType.Freelancer, x => x.FreelanceBidIndustries.Any(bi => bidParticipantWorkingSectorsIds.Contains(bi.FreelanceWorkingSectorId)) && x.BidStatusId == (int)TenderStatus.Open)
                .Select(x => new { Bid = x, Order = 1 });

            var bidsNotRelatedToCompaniesIndustries = bids
                .WhereIf(user.UserType == UserType.Provider, x => !(x.Bid_Industries.Any(bi => bidParticipantWorkingSectorsIds.Contains(bi.CommercialSectorsTreeId.Value)) && x.BidStatusId == (int)TenderStatus.Open))
                .WhereIf(user.UserType == UserType.Freelancer, x => !(x.FreelanceBidIndustries.Any(bi => bidParticipantWorkingSectorsIds.Contains(bi.FreelanceWorkingSectorId)) && x.BidStatusId == (int)TenderStatus.Open))
                .Select(x => new { Bid = x, Order = 2 });

            bids = bidsRelatedToCompaniesIndustries.Union(bidsNotRelatedToCompaniesIndustries)
                .OrderBy(x => x.Order)
                .ThenByDescending(x => x.Bid.CreationDate)
                .Select(x => x.Bid);
            return bids;
        }

        private async Task MapAllBidsResult(ApplicationUser user, Company company, Association association, Donor donor, Freelancer freelancer, List<IndustryMiniModel> bidParticipantWorkingSectorsMiniData, List<Bid> result, List<GetMyBidResponse> bidsModels)
        {
            var bidsIds = bidsModels.Select(a => a.Id);
            var providerBids = await _providerBidRepository
                .Find(x => x.IsPaymentConfirmed && bidsIds.Contains(x.BidId))
                .Select(a => new ProviderBidsBasicData
                {
                    BidId = a.BidId,
                    EntityId = a.CompanyId ?? a.ManualCompanyId ?? a.FreelancerId ?? 0,
                    UserType = a.UserType,
                })
                .ToListAsync();

            await MapBidModelsBasicData(result, bidsModels, bidsIds);

            Dictionary<long, UserFavBidList> userFavBids = new();

            if (user != null)
                userFavBids = (await _userFavBidList.Find(x => x.UserId == user.Id).ToListAsync()).ToDictionary(x => x.BidId);

            foreach (var itm in bidsModels)
            {
                BidDonor bidDonor = null;
                var bid = result.FirstOrDefault(bid => bid.Id == itm.Id);

                if (bid.BidDonorId.HasValue)
                {
                    bidDonor = await _BidDonorRepository
                        .Find(a => a.Id == bid.BidDonorId, true, false)
                        .Include(a => a.Donor)
                        .FirstOrDefaultAsync();
                }

                if (bid.EntityType == UserType.Association)
                {
                    itm.EntityName = bid.Association.Association_Name;
                    itm.EntityImage = bid.Association.Image;
                    itm.EntityImageFileName = bid.Association.ImageFileName;
                }
                else if (bid.EntityType == UserType.Donor)
                {

                    itm.EntityName = bid.Donor.DonorName;
                    itm.EntityImage = bid.Donor.Image;
                    itm.EntityImageFileName = bid.Donor.ImageFileName;
                }
                else
                    throw new ArgumentException($"This User Type {bid.EntityType.ToString()} not handled here {nameof(BidServiceCore.MapAllBidsResult)}");

                itm.EntityType = bid.AssociationId.HasValue ? UserType.Association : UserType.Donor;
                itm.EntityId = bid.AssociationId.HasValue ? bid.AssociationId.Value : bid.DonorId.Value;
                itm.EntityLogoResponse = await _imageService.GetFileResponseEncrypted(itm.EntityImage);
                itm.EntityImage = null;

                if (user != null)
                {
                    if (user.UserType == UserType.Provider || user.UserType == UserType.Freelancer)
                        itm.hasProviderMatchIndustries = itm.BidMainClassificationId.Any(x => bidParticipantWorkingSectorsMiniData.Any(y => y.ParentId == x.ParentId));

                    itm.IsUserFavorite = userFavBids.ContainsKey(itm.Id);

                    if (user.UserType == UserType.Provider || user.UserType == UserType.Company)
                        itm.IsBuyCompetitionDocuments = company != null && providerBids.Any(x => x.BidId == itm.Id && x.EntityId == company.Id && x.UserType == UserType.Company) ? true : false;

                    if(user.UserType == UserType.Freelancer)
                        itm.IsBuyCompetitionDocuments = freelancer != null && providerBids.Any(x => x.BidId == itm.Id && x.EntityId == freelancer.Id && x.UserType == UserType.Freelancer) ? true : false;

                    else if (user.UserType == UserType.Association)
                    {
                        itm.IsOwner = (bid.AssociationId.HasValue) && bid.AssociationId == association.Id;

                        itm.DonorName = bid.BidDonorId.HasValue ? association.Id == bid.AssociationId ? bidDonor.Donor == null ?
                                    bidDonor.NewDonorName : bidDonor.Donor.DonorName : "" : "";
                    }
                    else if (user.UserType == UserType.Donor)
                    {
                        itm.IsOwner = (bid.DonorId.HasValue) && bid.DonorId == donor.Id;

                        itm.DonorName = (bid.BidDonorId.HasValue && bidDonor.DonorId == donor.Id) ? bidDonor.Donor.DonorName : "";
                    }
                    else if (user.UserType == UserType.SuperAdmin || user.UserType == UserType.Admin || user.UserType == UserType.SupportManager || user.UserType == UserType.SupportMember)
                    {
                        if (bid.EntityType == UserType.Association)
                        {
                            itm.DonorName = bidDonor is not null ? bid.Association.Id == bid.AssociationId ? bidDonor.Donor == null ?
                                    bidDonor.NewDonorName : bidDonor.Donor.DonorName : "" : "";
                        }
                        else if (bid.EntityType == UserType.Donor)
                        {
                            var don = bid.Donor;
                            itm.DonorName = bidDonor is not null ? don.Id == bid.EntityId ? bidDonor.Donor == null ?
                                    bidDonor.NewDonorName : bidDonor.Donor.DonorName : "" : "";
                        }
                    }
                }
            }
        }

        private async Task MapBidModelsBasicData(List<Bid> result, List<GetMyBidResponse> bidsModels, IEnumerable<long> bidsIds)
        {
            var bidRegions = await _bidRegionsRepository
                    .Find(a => bidsIds.Contains(a.BidId), true, false)
                    .Include(x => x.Region)
                    .ToListAsync();
            int regionCount = await _regionRepository.Find(a => true, true, false).CountAsync();

            foreach (var bid in result)
            {
                var model = bidsModels.FirstOrDefault(bidMod => bidMod.Id == bid.Id);
                var currentBidRegions = bidRegions
                    .Where(a => a.BidId == bid.Id)
                    .ToList();

                model.Regions = BidRegion.getAllRegionsAsListOfIds(currentBidRegions);
                model.RegionsNames = currentBidRegions.Select(b => b.Region.NameAr).ToList();

                if (model.RegionsNames.Count == regionCount)
                {
                    model.RegionsNames.Clear();
                    model.RegionsNames.Add(Constants.AllRegionsArabic);
                }

                var bidWorkingSectors = bid.GetBidWorkingSectors();
                model.BidMainClassificationId = bidWorkingSectors.Select(a => new BidMainClassificationIds { Id = a.Id, ParentId = a.ParentId }).ToList();
                model.BidMainClassificationNames = bidWorkingSectors.Select(i => new BidMainClassificationNames { Name = i.NameAr, ParentName = i?.Parent?.NameAr }).ToList();
            }
        }



        #region check if need refact 
        private IQueryable<Bid> GetPrivateBidsForCurrentAssociation(long associationId, long entityId)
        {
            if (associationId > 0)
            {
                return _bidRepository.Find(bid =>
                !bid.IsDeleted
                && ( (bid.EntityId == associationId && bid.EntityType == UserType.Association) || bid.SupervisingAssociationId == associationId )
                && bid.BidTypeId == (int)BidTypes.Private
                && !( (bid.BidStatusId == (int)TenderStatus.Pending && bid.AssociationId != entityId) || bid.BidStatusId == (int)TenderStatus.Cancelled) 
                );
            }
            return Enumerable.Empty<Bid>().AsQueryable();
        }
        private IQueryable<Bid> GetPrivateBidsForCurrentDonor(long donorId, long entityId)
        {
            if (donorId > 0)
            {
                return _bidRepository.Find(bid => 
                !bid.IsDeleted
                && bid.EntityId == donorId
                && bid.EntityType == UserType.Donor
                && bid.BidTypeId == (int)BidTypes.Private
                && !( (bid.BidStatusId == (int)TenderStatus.Pending && bid.BidSupervisingData.Count > 0 && bid.BidSupervisingData.First().DonorId != entityId)
                        || bid.BidStatusId == (int)TenderStatus.Cancelled)
                );
            }
            return Enumerable.Empty<Bid>().AsQueryable();
        }
        private IQueryable<Bid> GetPrivateBidsForSupervisorDonor(long donorId)
        {
            if (donorId > 0)
            {
                var data = _BidDonorRepository.Find(a =>  a.DonorId == donorId && a.DonorResponse != DonorResponse.Reject)
                    .Select(a => a.BidId);

                return _bidRepository.Find(a =>  data.Contains(a.Id) && !a.IsDeleted
                && a.BidTypeId == (int)BidTypes.Private);
            }
            return Enumerable.Empty<Bid>().AsQueryable();
        }
        #endregion

        public async Task<PagedResponse<List<ReadOnlyQuantitiesTableModel>>> GetBidQuantitiesTableNew(long bidId, int pageSize = 5, int pageNumber = 1)
        {
            try
            {
                var user = _currentUserService.CurrentUser;

                var isUserHasAccess = await checkIfParticipantCanAccessBidData(bidId, user);
                if (!isUserHasAccess.IsSucceeded)
                    return new PagedResponse<List<ReadOnlyQuantitiesTableModel>>(isUserHasAccess.HttpErrorCode, isUserHasAccess.Code);

                var bidQuantitiesTable = _bidQuantitiesTableRepository.Find(x => x.BidId == bidId);

                if (!await bidQuantitiesTable.AnyAsync())
                {
                    return new PagedResponse<List<ReadOnlyQuantitiesTableModel>>(null, pageNumber, pageSize);
                }
                int totalRecords = await bidQuantitiesTable.CountAsync();
                bidQuantitiesTable = bidQuantitiesTable.OrderByDescending(a => a.Id).Skip((pageNumber - 1) * pageSize).Take(pageSize);
                var model = _mapper.Map<List<ReadOnlyQuantitiesTableModel>>(await bidQuantitiesTable.ToListAsync());

                return new PagedResponse<List<ReadOnlyQuantitiesTableModel>>(model, pageNumber, pageSize, totalRecords);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = $"Bid ID = {bidId}",
                    ErrorMessage = "Failed to Get Bid Quantities Table!",
                    ControllerAndAction = "BidController/GetBidQuantitiesTable/{id}"
                });
                return new PagedResponse<List<ReadOnlyQuantitiesTableModel>>(
                       HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }
        public async Task<OperationResult<BuyTenderDocsPillModel>> GetBuyTenderDocsPillModel(long providerBidId)
        {
            try
            {
                var providerBid = await _providerBidRepository.Find(x => x.Id == providerBidId)
                                        .Include(a => a.Bid)
                                        .Include(x=>x.Freelancer)
                                        .Include(a => a.Company).ThenInclude(a => a.Region)
                                        .Include(a => a.Company.Neighborhood)
                                        .Include(a => a.ManualCompany)
                                        .Include(a => a.CouponUsagesHistory)
                                        .Include(a => a.AddOnUsagesHistory)
                                        .FirstOrDefaultAsync();
                if (providerBid is null)
                    return OperationResult<BuyTenderDocsPillModel>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.NOT_FOUND);

                var generalSettings = await _appGeneralSettingsRepository.FindOneAsync(x => true);
                var docsPillModel = new BuyTenderDocsPillModel();

                string transactionNumber = "";
                var paymentBasicData = new PaymentTransactionBasicData { TransactionNumber = providerBid.TransactionNumber, PaymentMethod = (Nafes.CrossCutting.Model.Enums.PaymentMethod)providerBid.PaymentMethodId };
                var payments = await _helperService.GetPaymentTransaction(new List<PaymentTransactionBasicData> { paymentBasicData });
                if (payments.Count > 0)
                    transactionNumber = payments.FirstOrDefault().TranRef;

                //var transactionNumber =providerBid.PaymentMethodId==(int)(Nafes.CrossCutting.Model.Enums.PaymentMethod.PayTabs)?
                //    await _payTabTransactionRepository.Find(x => x.CartId == providerBid.TransactionNumber).Select(x=>x.TranRef).FirstOrDefaultAsync():
                //    await _hyperPayTransactionRepository.Find(x=>x.TransactionReference==providerBid.TransactionNumber).Select(x=>x.TransactionId).FirstOrDefaultAsync();

                docsPillModel.BidName = providerBid.Bid.BidName;
                docsPillModel.Ref_Number = providerBid.Bid.Ref_Number;
                docsPillModel.TanafosTaxNumber = generalSettings.TanafosTaxNumber;
                docsPillModel.PillNumber = string.IsNullOrEmpty(providerBid.InvoiceId) ? "-" : providerBid.InvoiceId;
                docsPillModel.CommercialRecordNumber = providerBid.Company?.Commercial_record??providerBid.ManualCompany?.Commercial_record??providerBid.Freelancer?.FreelanceDocumentNumber;
                docsPillModel.UniqueNumber700 = providerBid.Company?.UniqueNumber700 ?? providerBid.ManualCompany?.UniqueNumber700;
                docsPillModel.CompanyName = providerBid.Company?.CompanyName??providerBid.ManualCompany?.CompanyName ?? providerBid.Freelancer?.Name;
                docsPillModel.CompanyRegion = providerBid?.Company?.Region?.NameAr ?? providerBid?.Freelancer?.Region?.NameAr;
                docsPillModel.BuyTenderDocsDate = providerBid.CreationDate.Date.ToString("dd/MM/yyyy");
                docsPillModel.ContactUsMobile = generalSettings.ContactUsMobile;
                docsPillModel.LogoBase64 = await _imageService.GetImage("Resources/staging-emails-templates/EmailTemplate/billlogo.png");
                docsPillModel.TenderDocsPrice = providerBid.Bid.Association_Fees + providerBid.Bid.Tanafos_Fees;
                docsPillModel.TenderDocsPriceFormatted = (providerBid.Bid.Association_Fees + providerBid.Bid.Tanafos_Fees).ToTwoDigitsAfterDecimalPointWithThousandSeperator();
                docsPillModel.ValueAddedTaxPercentage = (double)providerBid.TaxPercentage;
                docsPillModel.ValueAddedTaxAmount = providerBid.Price - (double)_helperService.GetPriceAfterDeductingPercentage((decimal) providerBid.Price,(double) providerBid.TaxPercentage);
                docsPillModel.ValueAddedTaxAmountFormatted = docsPillModel.ValueAddedTaxAmount.ToTwoDigitsAfterDecimalPointWithThousandSeperator();
                docsPillModel.TotalAmount = providerBid.Price;
                docsPillModel.TotalAmountFormatted = providerBid.Price.ToTwoDigitsAfterDecimalPointWithThousandSeperator();
                docsPillModel.PayTabsTransactionNumber = transactionNumber;
                docsPillModel.TaxRecordNumber = providerBid.Company?.HasTaxRecordNumber == true ? providerBid.Company.TaxRecordNumber : "-";
                docsPillModel.TanafosCompanyFullName = generalSettings.TanafosCompanyFullName;
                docsPillModel.TanafosCommericalRecordNo = generalSettings.TanafosCommericalRecordNo;
                docsPillModel.TanafosBuildingNo = generalSettings.TanafosBuildingNo;
                docsPillModel.TanafosStreetName = generalSettings.TanafosStreetName;
                docsPillModel.TanafosNeighborhoodName = generalSettings.TanafosNeighborhoodName;
                docsPillModel.TanafosCityName = generalSettings.TanafosCityName;
                docsPillModel.TanafosCountryName = generalSettings.TanafosCountryName;
                docsPillModel.TanafosPostalCode = generalSettings.TanafosPostalCode;
                docsPillModel.TanafosAdditionalAddressNo = generalSettings.TanafosAdditionalAddressNo;
                docsPillModel.TanafosAdditionalInfo = generalSettings.TanafosAdditionalInfo;
                docsPillModel.CouponDiscount = providerBid.CouponUsagesHistory?.DiscountQuantity ?? 0;
                docsPillModel.CouponDiscountFormatted = docsPillModel.CouponDiscount.ToTwoDigitsAfterDecimalPointWithThousandSeperator();
                docsPillModel.AddonsDiscount = providerBid.AddOnUsagesHistory is null ? 0 : providerBid.AddOnUsagesHistory.DiscountQuantity;
                docsPillModel.AddonsDiscountFormatted = docsPillModel.AddonsDiscount.ToTwoDigitsAfterDecimalPointWithThousandSeperator();
                docsPillModel.TotalPriceAfterDiscountFormatted = (docsPillModel.TenderDocsPrice - Convert.ToDouble(docsPillModel.CouponDiscount) - Convert.ToDouble(docsPillModel.AddonsDiscount)).ToTwoDigitsAfterDecimalPointWithThousandSeperator();
                docsPillModel.PostalCode = providerBid.Company?.PostalCode ?? providerBid.Freelancer?.PostalCode ?? "-";
                docsPillModel.CompanyNeighborhood = providerBid.Company?.Neighborhood?.NameAr ?? providerBid.Freelancer?.Neighborhood?.NameAr ?? "-";
                docsPillModel.CompanyBuildingNo = providerBid.Company?.BuildingNo.HasValue==true ? providerBid.Company?.BuildingNo.Value.ToString() : "-";
                docsPillModel.Saudi_Riyal = await _helperService.GetSaudiRiyalBase64Svg("#333");
                return OperationResult<BuyTenderDocsPillModel>.Success(docsPillModel);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = $"provider Bid Id  = {providerBidId}",
                    ErrorMessage = "Failed to Get Buy Tender Docs Pill Model!",
                    ControllerAndAction = "BuyTenderDocsPillPdfController/GenerateTenderDocsPillPDF/{ProviderBidId}"
                });
                return OperationResult<BuyTenderDocsPillModel>.Fail(HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }

        public async Task<PagedResponse<List<GetBidViewsModel>>> GetBidViews(long bidId, int pageSize, int pageNumber)
        {
            try
            {
                var user = _currentUserService.CurrentUser;
                if (user == null)
                    return new PagedResponse<List<GetBidViewsModel>>(HttpErrorCode.NotAuthenticated);

                var bidViewsLogQuery = _bidViewsLogRepository.Find(a => a.BidId == bidId, true, false)
                    .Include(x => x.Organization)
                    .AsQueryable();

                int countResult = await bidViewsLogQuery
                    .CountAsync();
                if(countResult == 0)
                    return new PagedResponse<List<GetBidViewsModel>>(new List<GetBidViewsModel>(), pageNumber, pageSize, countResult);

                var bidViewsList = await bidViewsLogQuery
                    .OrderBy(a => a.SeenDate)
                    .ApplyPaging(pageNumber, pageSize)
                    .ToListAsync();

                var associationsIds = bidViewsList.Where(x => x.OrganizationId.HasValue && x.Organization.OrgTypeID == OrganizationType.Assosition).Select(x => x.Organization.EntityID).ToList();
                var donorsIds = bidViewsList.Where(x => x.OrganizationId.HasValue && x.Organization.OrgTypeID == OrganizationType.Donor).Select(x => x.Organization.EntityID).ToList();
                var companiesIds = bidViewsList.Where(x => x.OrganizationId.HasValue && x.Organization.OrgTypeID == OrganizationType.Comapny).Select(x => x.Organization.EntityID).ToList();
                var freelancersIds = bidViewsList.Where(x => x.OrganizationId.HasValue && x.Organization.OrgTypeID == OrganizationType.Freelancer).Select(x => x.Organization.EntityID).ToList();

                var associations = await _associationRepository.Find(a => associationsIds.Contains(a.Id))
                    .Select(x => new {Id = x.Id, Image = x.Image, ImageFileName = x.ImageFileName})
                    .ToListAsync();
                var donors = await _donorRepository.Find(a => donorsIds.Contains(a.Id))
                    .Select(x => new { Id = x.Id, Image = x.Image, ImageFileName = x.ImageFileName })
                    .ToListAsync();
                var companies = await _companyRepository.Find(a => companiesIds.Contains(a.Id))
                    .Select(x => new { Id = x.Id, Image = x.Image, ImageFileName = x.ImageFileName })
                    .ToListAsync();
                var freelancers = await _freelancerRepository.Find(a => companiesIds.Contains(a.Id))
                    .Select(x => new { Id = x.Id, Image = x.ProfileImageFilePath, ImageFileName = x.ProfileImageFileName })
                    .ToListAsync();

                List<GetBidViewsModel> model = new List<GetBidViewsModel>();
                FileResponse logoResponse = new FileResponse();
                foreach (var item in bidViewsList)
                {
                    if(!item.OrganizationId.HasValue) // Anonymous
                        logoResponse = await _imageService.GetFileResponseEncrypted(fileSettings.Tanafos_Logo_FilePath);
                    else if (item.Organization.OrgTypeID == OrganizationType.Assosition)
                    {
                        var association = associations.FirstOrDefault(a => a.Id == item.Organization.EntityID);
                        logoResponse = association is not null ? await _imageService.GetFileResponseEncrypted(association.Image, association.ImageFileName) : null;
                    }
                    else if (item.Organization.OrgTypeID == OrganizationType.Donor)
                    {
                        var donor = donors.FirstOrDefault(a => a.Id == item.Organization.EntityID);
                        logoResponse = donor is not null ? await _imageService.GetFileResponseEncrypted(donor.Image, donor.ImageFileName) : null;
                    }
                    else if (item.Organization.OrgTypeID == OrganizationType.Comapny)
                    {
                        var company = companies.FirstOrDefault(a => a.Id == item.Organization.EntityID);
                        logoResponse = company is not null ? await _imageService.GetFileResponseEncrypted(company.Image, company.ImageFileName) : null;
                    }
                    else if (item.Organization.OrgTypeID == OrganizationType.Freelancer)
                    {
                        var freelancer = freelancers.FirstOrDefault(a => a.Id == item.Organization.EntityID);
                        logoResponse = freelancer is not null ? await _imageService.GetFileResponseEncrypted(freelancer.Image, freelancer.ImageFileName) : null;
                    }
                    else
                        throw new ArgumentException($"This Organization Type {item.Organization.OrgTypeID} Not Handled Here {nameof(IBidService.GetBidViews)}");

                    model.Add(new GetBidViewsModel
                    {
                            OrganizationName = item.OrganizationId.HasValue ? item.Organization.OrgArName : "زائر",
                            OrganizationTypeId = item.OrganizationId.HasValue ? item.Organization.OrgTypeID : null,
                            seenDate = item.SeenDate,
                            OrganizationLogo = logoResponse,
                    });
                }
                return new PagedResponse<List<GetBidViewsModel>>(model, pageNumber, pageSize, countResult);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = $"Bid ID = {bidId}",
                    ErrorMessage = "Failed to Get Bid Views!",
                    ControllerAndAction = "BidController/GetBidViews/{bidId}"
                });
                return new PagedResponse<List<GetBidViewsModel>>(HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }

        }
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
                    var donor = await GetDonorUser(currentUser);
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
        public async Task<OperationResult<AddBidResponse>> AddInstantBid(AddInstantBid addInstantBidRequest)
        {
            try
            {
                var usr = _currentUserService.CurrentUser;

                var authorizedTypes = new List<UserType>() { UserType.Association, UserType.Donor, UserType.SuperAdmin, UserType.Admin };
                if (usr == null || !authorizedTypes.Contains(usr.UserType))
                    return OperationResult<AddBidResponse>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);

                if(addInstantBidRequest.BidType != BidTypes.Instant && addInstantBidRequest.BidType != BidTypes.Freelancing)
                    return OperationResult<AddBidResponse>.Fail(HttpErrorCode.InvalidInput, CommonErrorCodes.INVALID_INPUT);


                if (!addInstantBidRequest.IsDraft && validateAddInstantBidRequest(addInstantBidRequest, out var requiredParams))
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
                    return await EditInstantBid(addInstantBidRequest, usr, bidTypeBudget);

                return await AddInstantBid(addInstantBidRequest, usr, bidTypeBudget);
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

        private static bool validateAddInstantBidRequest(AddInstantBid addInstantBidRequest, out string requiredparams)
        {
            Dictionary<string, Predicate<AddInstantBid>> predicates = new Dictionary<string, Predicate<AddInstantBid>>()
            {
                { "addInstantBidRequest",addInstantBidRequest=>addInstantBidRequest is null },
                { "IndustriesIds",addInstantBidRequest=>addInstantBidRequest.IndustriesIds is null||addInstantBidRequest.IndustriesIds.Count == 0 } ,
                {"Objective", addInstantBidRequest=>string.IsNullOrEmpty(addInstantBidRequest.Objective) },
                {"BidTypeBudgetId", addInstantBidRequest=>addInstantBidRequest.BidTypeBudgetId == default },

            };
            var requiredValuesValidationResult = predicates.Where(x => x.Value(addInstantBidRequest)).Select(x => x.Key);
            requiredparams = string.Join(",", requiredValuesValidationResult);
            return (addInstantBidRequest is null || addInstantBidRequest.IndustriesIds is null ||
                                    addInstantBidRequest.IndustriesIds.Count == 0 || string.IsNullOrEmpty(addInstantBidRequest.Objective) ||
                                    addInstantBidRequest.BidTypeBudgetId == default);
        }

        private async Task<OperationResult<AddBidResponse>> AddInstantBid(AddInstantBid addInstantBidRequest, ApplicationUser usr, BidTypesBudgets bidTypeBudget)
        {
            var generalSettings = (await _appGeneralSettingService.GetAppGeneralSettings()).Data;
            var bid = _mapper.Map<Bid>(addInstantBidRequest);
            var association = (await _associationService.GetUserAssociation(usr.Email));
            var donor = (await _donorService.GetUserDonor(usr.Email));

            if (association is not null && association.RegistrationStatus != RegistrationStatus.Completed && association.RegistrationStatus != RegistrationStatus.AboutToExpire)
                return OperationResult<AddBidResponse>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.YOU_MUST_COMPLETE_SUBSCRIBTION_FIRST);
            if (donor is not null && donor.RegistrationStatus != RegistrationStatus.Completed && donor.RegistrationStatus != RegistrationStatus.AboutToExpire)
                return OperationResult<AddBidResponse>.Fail(HttpErrorCode.InvalidInput, BidErrorCodes.YOU_MUST_COMPLETE_SUBSCRIBTION_FIRST);

            bid.Tanafos_Fees = bidTypeBudget is null ? 0 : Convert.ToDouble(bidTypeBudget.BidDoumentsPrice);
            bid.EntityId = usr.CurrentOrgnizationId;
            bid.EntityType = usr.UserType;
            bid.Bid_Documents_Price = ((bid.Tanafos_Fees * (generalSettings.VATPercentage / 100)) + bid.Tanafos_Fees);
            bid.AssociationId = association is null ? null : association.Id;
            bid.DonorId = donor is null ? null : donor.Id;
            string firstPart_Ref_Number = DateTime.Now.ToString("yy") + DateTime.Now.ToString("MM") + ((int)BidTypes.Instant).ToString();
            string randomNumber = await GenerateBidRefNumber(addInstantBidRequest.Id, firstPart_Ref_Number);
            bid.Ref_Number = randomNumber;
            bid.BidTypeId = (int)addInstantBidRequest.BidType;
            bid.BidVisibility = addInstantBidRequest.BidType;
            bid.BidStatusId = (int)TenderStatus.Draft;
            bid.BidOffersSubmissionTypeId = (int)BidOffersSubmissionTypes.TechnicalAndFinancialOfferTogether;
            bid.IsBidAssignedForAssociationsOnly = addInstantBidRequest.IsBidAssignedForAssociationsOnly;

            await _bidRepository.Add(bid);

            if (usr.UserType == UserType.Donor)
            {
                var res = await this.AddInvitationToAssocationByDonorIfFound(addInstantBidRequest.InvitedAssociationByDonor, bid, addInstantBidRequest.IsAssociationFoundToSupervise, addInstantBidRequest.SupervisingAssociationId);
                if (!res.IsSucceeded)
                {
                    await this._bidRepository.Delete(bid);
                    return OperationResult<AddBidResponse>.Fail(res.HttpErrorCode, res.Code);
                }
            }

            await AddBidRegions(addInstantBidRequest.RegionsId, bid.Id);

            var parentIgnoredCommercialSectorIds = await _helperService.DeleteParentSectorsIdsFormList(addInstantBidRequest.IndustriesIds, addInstantBidRequest.BidType);
            await MapIndustries(addInstantBidRequest, usr, bid, parentIgnoredCommercialSectorIds);

            if (addInstantBidRequest.IsFunded)
            {
                var res = await SaveBidDonor(addInstantBidRequest.DonorRequest, bid.Id, usr.Id);
                if (!res.IsSucceeded)
                    return OperationResult<AddBidResponse>.Fail(res.HttpErrorCode, res.Code);
            }
            var entityName = bid.EntityType == UserType.Association ? association.Association_Name : donor.DonorName;
            await SendNewDraftBidEmailToSuperAdmins(bid, entityName);
            return OperationResult<AddBidResponse>.Success(new AddBidResponse()
            {
                BidVisibility = bid.BidVisibility,
                Id = bid.Id,
                Ref_Number = bid.Ref_Number
            });
        }

        private async Task<OperationResult<AddBidResponse>> EditInstantBid(AddInstantBid addInstantBidRequest, ApplicationUser usr, BidTypesBudgets bidTypeBudget)
        {
            var bid = await _bidRepository
                .Find(x => x.Id == addInstantBidRequest.Id, false,
                nameof(Bid.BidSupervisingData)).IncludeBasicBidData().AsNoTracking().FirstOrDefaultAsync();

            if (bid is null)
                return OperationResult<AddBidResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.NOT_FOUND);

            if (bid.EntityType != UserType.SuperAdmin
                && (bid.EntityId != usr.CurrentOrgnizationId || bid.EntityType != usr.UserType)
                && (usr.UserType != UserType.SuperAdmin && usr.UserType != UserType.Admin))
                return OperationResult<AddBidResponse>.Fail(HttpErrorCode.NotAuthorized, CommonErrorCodes.NotAuthorized);


            if (usr.UserType == UserType.SuperAdmin || usr.UserType == UserType.Donor || usr.UserType == UserType.Admin)
            {
                var res = await this.AddInvitationToAssocationByDonorIfFound(addInstantBidRequest.InvitedAssociationByDonor, bid, addInstantBidRequest.IsAssociationFoundToSupervise, addInstantBidRequest.SupervisingAssociationId);
                if (!res.IsSucceeded)
                    return OperationResult<AddBidResponse>.Fail(res.HttpErrorCode, res.Code);
            }

            bool isBidAllowedLimitedOffersCountChanged = bid.limitedOffers < addInstantBidRequest.limitedOffers 
                && bid.BidStatusId == (int)TenderStatus.Open
                &&bid.BidTypeId==(int) BidTypes.Instant;
            _mapper.Map(addInstantBidRequest, bid);

            var generalSettings = (await _appGeneralSettingService.GetAppGeneralSettings()).Data;
            if (await CheckIfWeCanUpdatePriceOfBid(usr, bid))
            {
                bid.Tanafos_Fees = bidTypeBudget is null ? 0 : Convert.ToDouble(bidTypeBudget.BidDoumentsPrice);
                bid.Bid_Documents_Price = ((bid.Tanafos_Fees * (generalSettings.VATPercentage / 100)) + bid.Tanafos_Fees);
            }

            bid.BidStatusId = addInstantBidRequest.IsDraft ? (int)TenderStatus.Draft : bid.BidStatusId;
            bid.BidOffersSubmissionTypeId = (int)BidOffersSubmissionTypes.TechnicalAndFinancialOfferTogether;
            bid.IsBidAssignedForAssociationsOnly = addInstantBidRequest.IsBidAssignedForAssociationsOnly;
            bid.BidDonorId = !addInstantBidRequest.IsFunded ? null : bid.BidDonorId;

            await _bidRepository.Update(bid);
            await UpdateBidRegions(addInstantBidRequest.RegionsId, bid.Id);

            var parentIgnoredCommercialSectorIds = await _helperService.DeleteParentSectorsIdsFormList(addInstantBidRequest.IndustriesIds, addInstantBidRequest.BidType);

            var newBidIndustrySet = new HashSet<long>(parentIgnoredCommercialSectorIds);
            var oldBidWorkingSectorSet =  new HashSet<long>(bid.GetBidWorkingSectors().Select(x => x.Id));

            if (!oldBidWorkingSectorSet.SetEquals(newBidIndustrySet))
                await MapIndustries(addInstantBidRequest, usr, bid, parentIgnoredCommercialSectorIds);

            if (addInstantBidRequest.IsFunded)
            {
                var res = await SaveBidDonor(addInstantBidRequest.DonorRequest, bid.Id, usr.Id);
                if (!res.IsSucceeded)
                    return OperationResult<AddBidResponse>.Fail(res.HttpErrorCode, res.Code);
            }
            else
            {
                var oldBidDonors = await _BidDonorRepository.FindAsync(x => x.BidId == bid.Id);
                if (oldBidDonors.Any())
                    await _BidDonorRepository.DeleteRangeAsync(oldBidDonors.ToList());
            }

            if ((usr.UserType == UserType.SuperAdmin || usr.UserType == UserType.Admin) && isBidAllowedLimitedOffersCountChanged)
                await SendEmailToCompaniesLimitedOffersChanged(bid);

            return OperationResult<AddBidResponse>.Success(new AddBidResponse()
            {
                BidVisibility = bid.BidVisibility,
                Id = bid.Id,
                Ref_Number = bid.Ref_Number
            });
        }

        private async Task MapIndustries(AddInstantBid addInstantBidRequest, ApplicationUser usr, Bid bid, List<long> parentIgnoredCommercialSectorIds)
        {
            if (addInstantBidRequest.IndustriesIds is null || addInstantBidRequest.IndustriesIds.Count == 0)
                return;

            if ((BidTypes)bid.BidTypeId == BidTypes.Freelancing)
                await AddUpdateBidFreelanceWorkingSectors(usr, bid, parentIgnoredCommercialSectorIds);
            else
                await AddUpdateBidCommercialSectors(usr, bid, parentIgnoredCommercialSectorIds);
        }

        private async Task AddUpdateBidCommercialSectors(ApplicationUser usr, Bid bid, List<long> parentIgnoredCommercialSectorIds)
        {
            List<Bid_Industry> bidIndustries = new List<Bid_Industry>();
            foreach (var cid in parentIgnoredCommercialSectorIds)
            {
                var bidIndustry = new Bid_Industry
                {
                    BidId = bid.Id,
                    CommercialSectorsTreeId = cid,
                    CreatedBy = usr.Id,
                    CreationDate = _dateTimeZone.CurrentDate,
                };
                bidIndustries.Add(bidIndustry);
            }

            if (bid.Bid_Industries != null && bid.Bid_Industries.Count != 0)
            {
                bid.Bid_Industries.ToList().ForEach(x => x.CommercialSectorsTree = null);
                await _bidIndustryRepository.DeleteRangeAsync(bid.Bid_Industries.ToList());
            }
            await _bidIndustryRepository.AddRange(bidIndustries);
        }

        private async Task AddUpdateBidFreelanceWorkingSectors(ApplicationUser usr, Bid bid, List<long> parentIgnoredCommercialSectorIds)
        {
            List<FreelanceBidIndustry> bidIndustries = new List<FreelanceBidIndustry>();
            foreach (var cid in parentIgnoredCommercialSectorIds)
            {
                var bidIndustry = new FreelanceBidIndustry
                {
                    BidId = bid.Id,
                    FreelanceWorkingSectorId = cid,
                    CreatedBy = usr.Id,
                    CreationDate = _dateTimeZone.CurrentDate,
                };
                bidIndustries.Add(bidIndustry);
            }

            if (bid.FreelanceBidIndustries != null && bid.FreelanceBidIndustries.Count != 0)
            {
                bid.FreelanceBidIndustries.ForEach(x => x.FreelanceWorkingSector = null);
                await _freelanceBidIndustryRepository.DeleteRangeAsync(bid.FreelanceBidIndustries);
            }
            await _freelanceBidIndustryRepository.AddRange(bidIndustries);
        }

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

                if (IsCurrentUserBidCreator(usr, bid))
                    return OperationResult<AddInstantBidAttachmentResponse>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);

                var ValidationResponse = await ValidateAddBidAttachmentsRequest(addInstantBidsAttachmentsRequest, bid, usr);
                if (!ValidationResponse.IsSucceeded)
                    return ValidationResponse;

                List<BidAttachment> bidAttachmentsToSave = await MapInstantBidAttachments(addInstantBidsAttachmentsRequest, bid);
                bid.BidStatusId = addInstantBidsAttachmentsRequest.BidStatusId != null && addInstantBidsAttachmentsRequest.BidStatusId > 0 ?
                    Convert.ToInt32(addInstantBidsAttachmentsRequest.BidStatusId) : (int)TenderStatus.Reviewing;//approved

                var bidDonor = await _donorService.GetBidDonorOfBidIfFound(bid.Id);
                var supervisingDonorClaims = await _donorService.GetFundedDonorSupervisingServiceClaims(bid.Id);


                bid.BidStatusId = CheckIfWeShouldMakeBidAtReviewingStatus(addInstantBidsAttachmentsRequest, usr, oldStatusOfbid) ? (int)TenderStatus.Reviewing
                    : addInstantBidsAttachmentsRequest.BidStatusId;
                bid.BidStatusId = CheckIfWasDraftAndChanged(addInstantBidsAttachmentsRequest.BidStatusId.Value, oldStatusOfbid)
                    && Constants.AdminstrationUserTypesWithoutSupport.Contains(usr.UserType) ? (int)TenderStatus.Open : bid.BidStatusId;

                if (CheckIfWeCanPublishBid(bid, oldStatusOfbid, bidDonor, supervisingDonorClaims))
                {

                    bid.CreationDate = _dateTimeZone.CurrentDate;
                    await DoBusinessAfterPublishingBid(bid, usr);

                    await _pointEventService.AddPointEventUsageHistoryAsync(new AddPointEventUsageHistoryModel
                    {
                        PointType = PointTypes.PublishNonDraftBid,
                        ActionId = bid.Id,
                        EntityId = bid.AssociationId.HasValue ? bid.AssociationId.Value : bid.DonorId.Value,
                        EntityUserType = bid.AssociationId.HasValue ? UserType.Association : UserType.Donor,
                    });

                    await LogBidCreationEvent(bid);
                }
                else if (bid.IsFunded && addInstantBidsAttachmentsRequest.BidStatusId != (int)TenderStatus.Draft && supervisingDonorClaims.Data.Any(x => x.ClaimType == SupervisingServiceClaimCodes.clm_3057 && x.IsChecked))
                {
                    await SendBidToSponsorDonorToBeConfirmed(usr, bid, bidDonor);
                }
                await _bidRepository.Update(bid);
                if (!CheckIfHasSupervisor(bid, supervisingDonorClaims) && CheckIfWeShouldSendPublishBidRequestToAdmins(bid, oldStatusOfbid))
                    await SendPublishBidRequestEmailAndNotification(usr, bid, oldStatusOfbid);

                if (usr.UserType == UserType.SuperAdmin || usr.UserType == UserType.Admin)
                {
                    if (bid.BidTypeId != (int)BidTypes.Private && oldStatusOfbid == TenderStatus.Draft && addInstantBidsAttachmentsRequest.BidStatusId == (int)TenderStatus.Open)
                    {
                        await AddSystemReviewToBidByCurrentUser(bid.Id, SystemRequestStatuses.Accepted);  //Add approval review to bid incase of attachments are added by admin and bid type is not private.

                        await ExecutePostPublishingLogic(bid, usr, oldStatusOfbid);


                    }
                    if (addInstantBidsAttachmentsRequest.IsSendEmailsAndNotificationAboutUpdatesChecked)
                        await SendUpdatedBidEmailToCreatorAndProvidersOfThisBid(bid);
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

        private static bool CheckIfWeShouldMakeBidAtReviewingStatus(AddInstantBidsAttachments addInstantBidsAttachmentsRequest, ApplicationUser usr, TenderStatus oldStatusOfbid)
        {
            var isEntity = usr.UserType == UserType.Association || usr.UserType == UserType.Donor;
            return CheckIfWasDraftAndChanged(addInstantBidsAttachmentsRequest.BidStatusId.Value, oldStatusOfbid)
                                || (isEntity && addInstantBidsAttachmentsRequest.BidStatusId != (int)TenderStatus.Draft);
        }

        private static bool CheckIfWeCanPublishBid(Bid bid, TenderStatus oldStatusOfbid, BidDonor bidDonor, OperationResult<List<GetDonorSupervisingServiceClaimsResponse>> supervisingDonorClaims)
        {
            return CheckIfWasDraftAndBecomeOpen(bid, oldStatusOfbid) &&
                                (bidDonor is null || (bidDonor is not null && supervisingDonorClaims.Data is not null &&
                                supervisingDonorClaims.Data.Any(x => x.ClaimType == SupervisingServiceClaimCodes.clm_3057 && !x.IsChecked)));
        }

        private static bool CheckIfWasDraftAndChanged(int bidStatus, TenderStatus oldStatusOfbid)
        {
            return oldStatusOfbid == TenderStatus.Draft && bidStatus != (int)TenderStatus.Draft;
        }

        private bool IsCurrentUserBidCreator(ApplicationUser usr, Bid bid)
        {
            return (usr.UserType == UserType.Association || usr.UserType == UserType.Donor) && (bid.EntityType != usr.UserType || bid.EntityId != usr.CurrentOrgnizationId);

        }
        private async Task LogBidCreationEvent(Bid bid)
        {
            //===============log event===============
            var industries = bid.Bid_Industries.Select(a => a.CommercialSectorsTree.NameAr).ToList();
            string[] styles = await _helperService.GetEventStyle(EventTypes.BidCreation);
            await _helperService.LogBidEvents(new BidEventModel
            {
                BidId = bid.Id,
                BidStatus = (TenderStatus)bid.BidStatusId,
                BidEventSection = BidEventSections.Bid,
                BidEventTypeId = (int)EventTypes.BidCreation,
                EventCreationDate = _dateTimeZone.CurrentDate,
                ActionId = bid.Id,
                Audience = AudienceTypes.All,
                Header = string.Format(styles[0], fileSettings.ONLINE_URL, bid.Donor == null ? "association" : "donor", bid.EntityId, bid.Donor == null ? bid.Association.Association_Name : bid.Donor.DonorName, bid.CreationDate.ToString("dddd d MMMM، yyyy , h:mm tt", new CultureInfo("ar-AE"))),
                Notes1 = string.Format(styles[1], string.Join("،", industries))
            });
        }

        private static bool CheckIfWasDraftAndBecomeOpen(Bid bid, TenderStatus oldStatusOfbid)
        {
            return bid.BidStatusId == (int)TenderStatus.Open && oldStatusOfbid == TenderStatus.Draft;
        }
        private async Task<List<BidAttachment>> MapInstantBidAttachments(AddInstantBidsAttachments addInstantBidsAttachmentsRequest, Bid bid)
        {
            var existingAttachments_ContactList = await _bidAttachmentRepository.
                Find(x => x.BidId == addInstantBidsAttachmentsRequest.BidId).ToListAsync();
            await _bidAttachmentRepository.DeleteRangeAsync(existingAttachments_ContactList);

            var bidAttachmentsToSave = new List<BidAttachment>();
            if (addInstantBidsAttachmentsRequest.LstAttachments != null && addInstantBidsAttachmentsRequest.LstAttachments.Count > 0)
            {

                bidAttachmentsToSave = addInstantBidsAttachmentsRequest.LstAttachments.Select(attachment =>
                    new BidAttachment
                    {
                        BidId = bid.Id,
                        AttachmentName = attachment.AttachmentName,
                        AttachedFileURL = _encryptionService.Decrypt(attachment.AttachedFileURL),
                        IsDeleted = false
                    }).ToList();


                await _bidAttachmentRepository.AddRange(bidAttachmentsToSave);

            }
            bidAttachmentsToSave.ForEach(file => file.AttachedFileURL = _encryptionService.Encrypt(file.AttachedFileURL));
            return bidAttachmentsToSave;
        }

        private async Task<OperationResult<AddInstantBidAttachmentResponse>> ValidateAddBidAttachmentsRequest
            (AddInstantBidsAttachments addInstantBidsAttachmentsRequest, Bid bid, ApplicationUser usr)
        {

            var authorizedTypes = new List<UserType>() { UserType.Association, UserType.SuperAdmin, UserType.Donor, UserType.Admin };
            if (usr == null || !authorizedTypes.Contains(usr.UserType))
                return OperationResult<AddInstantBidAttachmentResponse>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);


            if (bid == null)
                return OperationResult<AddInstantBidAttachmentResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);

            if (usr.UserType == UserType.Association)
            {
                var association = await _associationService.GetUserAssociation(usr.Email);
                if (association == null)
                    return OperationResult<AddInstantBidAttachmentResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.ASSOCIATION_NOT_FOUND);


                if (bid.EntityId != usr.CurrentOrgnizationId || bid.EntityType != usr.UserType)
                    return OperationResult<AddInstantBidAttachmentResponse>.Fail(HttpErrorCode.NotAuthorized, CommonErrorCodes.NotAuthorized);
            }
            if (usr.UserType == UserType.Donor)
            {
                var donor = await GetDonorUser(usr);
                if (donor is null)
                    return OperationResult<AddInstantBidAttachmentResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.DONOR_NOT_FOUND);
            }

            var checkQuantitesTableForThisBid = await _bidQuantitiesTableRepository.Find(a => a.BidId == bid.Id).AnyAsync();
            if (!checkQuantitesTableForThisBid)
                return OperationResult<AddInstantBidAttachmentResponse>.Fail(HttpErrorCode.Conflict, CommonErrorCodes.PLEASE_FILL_ALL_REQUIRED_DATA_IN_PREVIOUS_STEPS);
            if (addInstantBidsAttachmentsRequest.BidStatusId.HasValue && CheckIfWasDraftAndChanged(addInstantBidsAttachmentsRequest.BidStatusId.Value, (TenderStatus)bid.BidStatusId) && !bid.CanPublishBid())
                return OperationResult<AddInstantBidAttachmentResponse>.Fail(HttpErrorCode.InvalidInput, CommonErrorCodes.PLEASE_FILL_ALL_REQUIRED_DATA_IN_PREVIOUS_STEPS);

            return OperationResult<AddInstantBidAttachmentResponse>.Success(null);
        }

        public async Task<OperationResult<List<InvitedCompanyResponseDto>>> GetAllInvitedCompaniesForBidAsync(GetAllInvitedCompaniesRequestModel request)
        {
            try
            {
                if (_currentUserService.IsUserNotAuthorized(new List<UserType> { UserType.SuperAdmin, UserType.Admin, UserType.Association, UserType.Donor }))
                    return OperationResult<List<InvitedCompanyResponseDto>>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);

                if (request.BidId is null || request.BidId <= 0)
                    return OperationResult<List<InvitedCompanyResponseDto>>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);

                return OperationResult<List<InvitedCompanyResponseDto>>.Success(await GetAllInvitedCompaniesResponseForBid(request));
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = request,
                    ErrorMessage = "Failed Get All Companies To Invite Response!",
                    ControllerAndAction = "Company/all-companies-to-invite"
                });
                return new PagedResponse<List<InvitedCompanyResponseDto>>(HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }

        private async Task<List<InvitedCompanyResponseDto>> GetAllInvitedCompaniesResponseForBid(GetAllInvitedCompaniesRequestModel request)
        {
            var invitedCompanies = _bidInvitationsRepository
                    .Find(c => c.BidId == request.BidId)
                    .Include(x => x.Company)
                    .Include(x => x.ManualCompany)
                    .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.CompanyName))
                invitedCompanies = invitedCompanies.Where(c => c.Company.CompanyName.Contains(request.CompanyName) || c.ManualCompany.CompanyName.Contains(request.CompanyName));

            if (!string.IsNullOrWhiteSpace(request.CommercialNo))
                invitedCompanies = invitedCompanies.Where(c => c.Company.Commercial_record.Equals(request.CommercialNo) || c.ManualCompany.Commercial_record.Equals(request.CommercialNo) || c.Company.UniqueNumber700.Equals(request.CommercialNo) || c.ManualCompany.UniqueNumber700.Equals(request.CommercialNo));

            var result = await invitedCompanies
                .OrderByDescending(c => c.CreationDate)
                .AsSplitQuery()
                .ToListAsync();

            return GetAllInvitedCompaniesModels(result).ToList();
        }

        private IEnumerable<InvitedCompanyResponseDto> GetAllInvitedCompaniesModels(List<BidInvitations> allCompanies)
        {
            foreach (var inv in allCompanies)
            {
                yield return new InvitedCompanyResponseDto
                {
                    BidInvitationId = inv.Id,
                    CompanyId = inv.CompanyId is null ? 0 : inv.CompanyId,
                    ManualCompanyId = inv.ManualCompanyId is null ? 0 : inv.ManualCompanyId,
                    CompanyName = (inv.Company is null && inv.ManualCompany is null) ? inv.Email : ((inv.CompanyId.HasValue) ? inv.Company.CompanyName : inv.ManualCompany.CompanyName),
                    CommercialNo = inv.CommercialNo,
                    UniqueNumber700 = inv.UniqueNumber700 ?? inv.Company?.UniqueNumber700 ?? null,
                    InvitationStatus = inv.InvitationStatus,
                    InvitationStatusName = inv.InvitationStatus == InvitationStatus.Sent ? "تمت الدعوة" : "جديدة",
                    PhoneNumber = inv.PhoneNumber,
                    Email = inv.Email,
                    CreationDate = inv.CreationDate
                };
            }
        }

        private async Task UpdateBidRegions(List<int> regionsId, long bidId)
        {
            List<BidRegion> oldBidRegionsToBeDeleted = await _bidRegionsRepository.Find(bidReg => bidReg.BidId == bidId).ToListAsync();
            await _bidRegionsRepository.DeleteRangeAsync(oldBidRegionsToBeDeleted);

            await AddBidRegions(regionsId, bidId);
        }

        private async Task AddBidRegions(List<int> regionsId, long bidId)
        {
            if (regionsId.IsNullOrEmpty())
                return;
            List<BidRegion> bidRegions = BidRegion.cretaeListOfMe(regionsId, bidId);
            await _bidRegionsRepository.AddRange(bidRegions);
        }

        public async Task<OperationResult<bool>> TakeActionOnBidByDonor(long bidDonorId, DonorResponse donorResponse)
        {
            try
            {
                //======================check Authorization=============================
                var user = _currentUserService.CurrentUser;
                if (user == null || user.UserType != UserType.Donor)
                {
                    return OperationResult<bool>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);
                }

                BidDonor bidDonor = await _BidDonorRepository.FindOneAsync(x => x.Id == bidDonorId, false, nameof(BidDonor.Donor));
                //.Include(x => x.Donor)
                //.Include(x => x.Bid)
                //.Include(x => x.Bid.Association)
                //.FirstOrDefaultAsync();

                if (bidDonor is null)
                    return OperationResult<bool>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.THIS_BID_HAVE_NO_DONOR);

                if (bidDonor.DonorResponse == DonorResponse.Accept || bidDonor.DonorResponse == DonorResponse.Reject)
                    return OperationResult<bool>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.THIS_BID_ALREADY_MAKE_ACTION_BY_DONOR);

                //========================update response==========================
                bidDonor.DonorResponse = donorResponse;
                bidDonor.ModificationDate = _dateTimeZone.CurrentDate;
                bidDonor.ModifiedBy = user.Id;
                bool response = await _BidDonorRepository.Update(bidDonor);

                Bid bid = await _bidRepository
                    .Find(a => a.Id == bidDonor.BidId)
                    .IncludeBasicBidData()
                    .FirstOrDefaultAsync();

                //======================check is donor accept bid=============================
                if (donorResponse == DonorResponse.Accept)
                {
                    if (response)
                    {
                        bid.BidDonorId = bidDonorId;
                        await _bidRepository.Update(bid);
                    }
                }

                //======================check is donor reject bid=============================
                else
                {
                    if (response)
                    {
                        bid.IsFunded = false;
                        await _bidRepository.Update(bid);
                    }
                    //===============send email===========================

                    var emailModel = new DonorRejectToSuperviseBidEmail()
                    {
                        BaseBidEmailDto = await _helperService.GetBaseDataForBidsEmails(bid),
                        DonorName = bidDonor.Donor.DonorName
                    };
                    var emailRequest = new EmailRequest()
                    {
                        ControllerName = BaseBidEmailDto.BidsEmailsPath,
                        ViewName = DonorRejectToSuperviseBidEmail.EmailTemplateName,
                        ViewObject = emailModel,
                        To = await _associationService.GetEmailToSend(bid.AssociationId ?? 0, bid.Association.Manager_Email),
                        Subject = $"رفض منح منافسة {bid.BidName}",
                        SystemEventType = (int)SystemEventsTypes.DonorRejectToSuperviseBidEmail
                    };
                    await _emailService.SendAsync(emailRequest);

                    //================send Notifications===================
                    var donorUser = await _userManager.FindByEmailAsyncSafe(bidDonor.Donor.ManagerEmail);
                    var associationUsers = await _notificationUserClaim.GetUsersClaim(new string[] { AssociationClaimCodes.clm_3030.ToString() }, bid.AssociationId ?? 0, OrganizationType.Assosition);
                    var _notificationService = (INotificationService)_serviceProvider.GetService(typeof(INotificationService));
                    var notificationObj = await _notificationService.GetNotificationObjModelToSendAsync(new NotificationObjModel
                    {
                        EntityId = bidDonor.BidId,
                        Message = $"رفض {bidDonor.Donor.DonorName} منافسة {bid.BidName}",
                        ActualRecieverIds = associationUsers.ActualReceivers,
                        SenderId = donorUser?.Id,
                        NotificationType = NotificationType.DonorSupervisingBidRejected,
                        ServiceType = ServiceType.Bids,
                        SystemEventType = (int)SystemEventsTypes.RejectBidNotification
                    });

                    notificationObj.BidId = bidDonor.BidId;
                    notificationObj.BidName = bid.BidName;
                    notificationObj.SenderName = bidDonor.Donor.DonorName;
                    notificationObj.AssociationName = bidDonor.Donor.DonorName;
                    await _notificationService.SendRealTimeNotificationToUsersAsync(notificationObj, associationUsers.RealtimeReceivers.Select(a => a.ActualRecieverId).ToList(), (int)SystemEventsTypes.RejectBidNotification);
                }
                return OperationResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = $"BidDonorId = {bidDonorId},DonorResponse={donorResponse}",
                    ErrorMessage = "Failed to Take Action On Bid By Donor!",
                    ControllerAndAction = "BidController/TakeActionOnBidByDonor"
                });
                return OperationResult<bool>.Fail(HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }

        private async Task<OperationResult<bool>> AddInvitationToAssocationByDonorIfFound(InvitedAssociationByDonorModel model, Bid bid, bool IsAssociationFoundToSupervise, long? SupervisingAssociationId)
        {
            if (_currentUserService.IsUserNotAuthorized(new List<UserType> { UserType.Association, UserType.Donor, UserType.SuperAdmin, UserType.Admin }))
                return OperationResult<bool>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);
            var user = _currentUserService.CurrentUser;

            var invitedAssociation = await _invitedAssociationsByDonorRepository.FindOneAsync(inv => inv.BidId == bid.Id);

            if (IsAssociationFoundToSupervise == false)
            {
                if (invitedAssociation is not null)
                    await _invitedAssociationsByDonorRepository.Delete(invitedAssociation);
                bid.SupervisingAssociationId = null;
                bid.IsAssociationFoundToSupervise = false;
                bid.IsSupervisingAssociationInvited = false;
                await _bidRepository.Update(bid);
                return OperationResult<bool>.Success(true);
            }

            if (!SupervisingAssociationId.HasValue)
                return OperationResult<bool>.Fail(HttpErrorCode.InvalidInput, CommonErrorCodes.INVALID_INPUT);


            if (SupervisingAssociationId.Value > 0) // already created association.
            {
                var association = await _associationRepository.FindOneAsync(ass => ass.Id == SupervisingAssociationId.Value &&
                ass.IsDeleted == false
                && ass.isVerfied == true &&
                (ass.RegistrationStatus != RegistrationStatus.Rejected && ass.RegistrationStatus != RegistrationStatus.NotReviewed), false);
                if (association is null)
                    return OperationResult<bool>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.ASSOCIATION_NOT_FOUND);

                bid.IsSupervisingAssociationInvited = false;
                bid.SupervisingAssociationId = SupervisingAssociationId.Value;
            }
            else
            { // invited association.
                if (model is null)
                    return OperationResult<bool>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.ASSOCIATION_INVITATION_NOT_FOUND);

                if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Registry_Number) ||
                string.IsNullOrEmpty(model.AssociationName))
                {
                    bid.SupervisingAssociationId = null;
                    bid.IsAssociationFoundToSupervise = false;
                    bid.IsSupervisingAssociationInvited = false;
                    await _bidRepository.Update(bid);
                    return OperationResult<bool>.Success(true);
                }

                var isAssociationRegistrerdBeore = await _helperService.CheckIfAssociationRegisterationNumberIsNotFoundBeforeWithSameType(model.Registry_Number, (int)AssociationTypes.CivilAssociations);

                if (!isAssociationRegistrerdBeore.IsSucceeded)
                    return isAssociationRegistrerdBeore;

                if (invitedAssociation is not null && !model.isSameInformation(invitedAssociation))
                {
                    model.moveDataFromModelToEntity(invitedAssociation, user.Id);
                    await _invitedAssociationsByDonorRepository.Update(invitedAssociation);
                }

                if (invitedAssociation is null || !model.isSameInformation(invitedAssociation))
                    await _invitedAssociationsByDonorRepository.Add(model.createInvitedAssociationsByDonorFromMe(bid.Id, user));
                bid.IsSupervisingAssociationInvited = true;
                bid.SupervisingAssociationId = 0; // to reset Association Id until the invited association accepts the invitation.
            }

            bid.IsAssociationFoundToSupervise = IsAssociationFoundToSupervise;
            await _bidRepository.Update(bid);

            return OperationResult<bool>.Success(true);
        }
        private async Task<InvitedAssociationByDonorResponse> GetInvitedAssociationIfFound(Bid bid)
        {
            if (bid.IsAssociationFoundToSupervise == false || !bid.SupervisingAssociationId.HasValue || bid.SupervisingAssociationId.Value >= 0)
                return null;
            var invitedAssociation = await _invitedAssociationsByDonorRepository.FindOneAsync(inv => inv.BidId == bid.Id);
            return InvitedAssociationByDonorResponse.CreateObjectFromMe(invitedAssociation);

        }
 
        private async Task<Donor> GetDonorUser(ApplicationUser user)
        {
            if (user is null || user.UserType != UserType.Donor)
                return null;

            return await _donorRepository.FindOneAsync(don => don.Id == user.CurrentOrgnizationId &&
            don.isVerfied && !don.IsDeleted);

        }

        public async Task<string> GetBidCreatorName(Bid bid)
        {
            if (bid.EntityType == UserType.Association)
            {
                if (bid.Association is not null)
                    return bid.Association.Association_Name;

                return await _associationRepository.Find(a => a.Id == bid.EntityId).Select(d => d.Association_Name).FirstOrDefaultAsync();
            }
            else if (bid.EntityType == UserType.Donor)
            {
                if (bid.Donor is not null)
                    return bid.Donor.DonorName;

                return await _donorRepository.Find(a => a.Id == bid.EntityId).Select(d => d.DonorName).FirstOrDefaultAsync();
            }

            return string.Empty;
        }
        public async Task<string> GetBidCreatorEmailToReceiveEmails(Bid bid)
        {
            if (bid.EntityType == UserType.Association)
            {
                if (bid.Association is not null)
                    return await _associationService.GetEmailToSend(bid.AssociationId.Value, bid.Association.Manager_Email);
                var association = await _associationRepository.Find(a => a.Id == bid.EntityId).Select(d => new { d.Id, d.Manager_Email })
                    .FirstOrDefaultAsync();
                return await _associationService.GetEmailToSend(association.Id, association.Manager_Email);
            }
            else if (bid.EntityType == UserType.Donor)
            {
                if (bid.Donor is not null)
                    return await _donorService.GetEmailOfUserSelectedToReceiveEmails(bid.DonorId.Value, bid.Donor.ManagerEmail);
                var donor = await _donorRepository.Find(a => a.Id == bid.EntityId).Select(d => new { d.Id, d.ManagerEmail })
                  .FirstOrDefaultAsync();
                return await _donorService.GetEmailOfUserSelectedToReceiveEmails(donor.Id, donor.ManagerEmail);

            }

            return string.Empty;
        }
        public long GetBidCreatorId(Bid bid)
        {
            if (bid.EntityType == UserType.Association)
            {
                return (long)bid.AssociationId;
            }
            else if (bid.EntityType == UserType.Donor)
            {
                return bid.EntityId;
            }

            return 0;
        }

        public async Task<(string, string)> GetBidCreatorImage(Bid bid)
        {
            var imagePath = fileSettings.DefaultImage_Association_FilePath;
            var imageFileName = Path.GetFileName(fileSettings.DefaultImage_Association_FilePath);

            if (bid.EntityType == UserType.Association)
            {
                if (bid.Association is not null)
                    return (bid.Association.Image, bid.Association.ImageFileName);

                var res = await _associationRepository.Find(a => a.Id == bid.EntityId)
                .Select(d => new { d.Image, d.ImageFileName })
                .FirstOrDefaultAsync();

                if (res is not null)
                    return (res.Image, res.ImageFileName);
            }
            else if (bid.EntityType == UserType.Donor)
            {
                if (bid.Donor is not null)
                    return (bid.Donor.Image, bid.Donor.ImageFileName);

                var res = await _donorRepository.Find(a => a.Id == bid.EntityId)
                .Select(d => new { d.Image, d.ImageFileName })
                .FirstOrDefaultAsync();

                if (res is not null)
                    return (res.Image, res.ImageFileName);
            }
            return (imagePath, imageFileName);
        }

        public async Task<List<ProviderBid>> //providerBids, List<BidIdWithAssociationFees> bidIdWithAssociationFees)>
            GetProviderBidsWithAssociationFees(List<ProvidersBidsWithdrawModel> providerBidsIds, long creatorId, UserType creatorType)
        {
            var providerBidsIdsInRequest = providerBidsIds.Select(x => x.Id).ToHashSet();

            var providerBids = await _providerBidRepository
                .Find(p =>
                    providerBidsIdsInRequest.Contains(p.Id)
                    && p.IsPaymentConfirmed
                    //&& (p.Bid.Association_Fees + p.AssociationTaxesAmount) > 0
                    && ((string.IsNullOrEmpty(p.CouponHash) && ((p.Bid.Association_Fees + p.AssociationTaxesAmount) > 0)) || (!string.IsNullOrEmpty(p.CouponHash) && ((p.AssociationFeesAfterDiscount + p.AssociationTaxesAmount) > 0)))
                    && p.Bid.EntityId == creatorId && p.Bid.EntityType == creatorType, false).Include(a => a.Bid)
                .ToListAsync();

            //var bidsIds = providerBids.Select(a => a.BidId).Distinct().ToList();

            //var bids = await _bidRepository
            //    .Find(p =>
            //        bidsIds.Contains(p.Id)
            //        && !p.IsDeleted)
            //    .Select(a => new BidIdWithAssociationFees { BidId = a.Id, Association_Fees = a.Association_Fees })
            //    .AsNoTracking()
            //    .ToListAsync();

            return (providerBids); //, bids);
        }

        public async Task<OperationResult<bool>> InviteProvidersWithSameCommercialSectors(long bidId, bool isAutomatically = false)
        {
            try
            {
                var user = _currentUserService.CurrentUser;
                if (user is null || (user.UserType != UserType.SuperAdmin && user.UserType != UserType.Admin))
                    return OperationResult<bool>.Fail(HttpErrorCode.NotAuthorized, CommonErrorCodes.NotAuthorized);

                var bid = await _bidRepository.Find(x => !x.IsDeleted && x.Id == bidId && (x.BidTypeId == (int)BidTypes.Public || x.BidTypeId == (int)BidTypes.Instant || x.BidTypeId == (int)BidTypes.Freelancing))
                    .IncludeBasicBidData()
                    .FirstOrDefaultAsync();

                if (bid is null)
                    return OperationResult<bool>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);

                if ((TenderStatus)bid.BidStatusId != TenderStatus.Open)
                    return OperationResult<bool>.Fail(HttpErrorCode.InvalidInput, CommonErrorCodes.YOU_CAN_DO_THIS_ACTION_ONLY_WHEN_BID_AT_OPEN_STATE);


                _backgroundQueue.QueueTask(async (ct) =>
                {
                    await InviteProvidersInBackground(bid, isAutomatically, user);

                });
                return OperationResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = bidId,
                    ErrorMessage = "Failed to Invite Providers With Same Industries!",
                    ControllerAndAction = "BidController/InviteProvidersWithSameIndustries"
                });
                return OperationResult<bool>.Fail(HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }

        private async Task InviteProvidersInBackground(Bid bid, bool isAutomatically, ApplicationUser user)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var helperService = scope.ServiceProvider.GetRequiredService<IHelperService>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                var entityName = bid.EntityType == UserType.Association ? bid.Association?.Association_Name : bid.Donor?.DonorName;

                var result = await SendEmailToCompaniesInBidIndustry(bid, entityName, isAutomatically);
                var addReviewedSystemRequestResult = await helperService.AddReviewedSystemRequestLog(new AddReviewedSystemRequestLogRequest
                {
                    EntityId = bid.Id,
                    SystemRequestStatus = SystemRequestStatuses.Accepted,
                    SystemRequestType = SystemRequestTypes.BidInviting,
                    Note = result.AllCount.ToString(),
                    Note2 = result.AllNotFreeSubscriptionCount.ToString(),
                    SystemRequestReviewers = isAutomatically ? SystemRequestReviewers.System : null
                }, user);

                await SendNotificationsOfBidAdded(user, bid, entityName);

                var notificationObj = new NotificationModel()
                {
                    BidId = bid.Id,
                    BidName = bid.BidName,
                    NotificationType = NotificationType.InviteProvidersWithSameIndustriesDone
                };
                await notificationService.SendRealTimeNotificationToUsersAsync(notificationObj, new List<string>() { user.Id });
            }
            catch (Exception ex)
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var loggerService = scope.ServiceProvider.GetRequiredService<ILoggerService<BidService>>();

                string refNo = loggerService.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = bid.Id,
                    ErrorMessage = "Failed to Invite Providers With Same Industries Bg!",
                    ControllerAndAction = "BidController/InviteProvidersWithSameIndustriesBg"
                });
            }
        }

        public async Task<OperationResult<GetProviderDataOfRefundableCompanyBidModel>> GetProviderDataOfRefundableCompanyBid(long companyBidId)
        {

            var providerRefundRequest = await _providerRefundTransactionRepository.Find(x => x.ProviderBidId == companyBidId && x.withdrawStatusId == WithdrawStatus.Success)
                .Include(x => x.ProviderBid)
                .Include(x => x.ProviderBid.Bid)
                .Include(x => x.ProviderBid.Company)
                .Include(x => x.ProviderBid.Freelancer)
                .Include(x => x.ProviderBid.Company.Region)
                .Include(x => x.ProviderBid.Freelancer.Region)
                .Include(x => x.ProviderBid.Company.Neighborhood)
                .Include(x => x.ProviderBid.Freelancer.Neighborhood)
                .Include(x => x.ProviderBid.Company.Provider)
                .FirstOrDefaultAsync();

            if (providerRefundRequest is null)
                return OperationResult<GetProviderDataOfRefundableCompanyBidModel>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.NOT_FOUND);

            var generalSettings = await _appGeneralSettingsRepository.FindOneAsync(x => true);

            var res = new GetProviderDataOfRefundableCompanyBidModel();

            res.CompanyName = providerRefundRequest.ProviderBid.GetBuyerTermsBookEntityName();
            res.CompanyCountryName = "المملكة العربية السعودية";
            res.CompanyCityName = providerRefundRequest.ProviderBid.Company?.Region?.NameAr ?? providerRefundRequest.ProviderBid.Freelancer?.Region?.NameAr ?? "-";
            res.CompanyPostalCode = providerRefundRequest.ProviderBid.Freelancer?.PostalCode ?? "-";
            res.CompanyNeighborhoodName = providerRefundRequest.ProviderBid.Company?.Neighborhood?.NameAr ?? providerRefundRequest.ProviderBid.Freelancer?.Neighborhood?.NameAr ?? "-";
            res.CompanyStreetName = providerRefundRequest.ProviderBid.Company?.Street ?? providerRefundRequest.ProviderBid.Freelancer?.Street ?? "-";
            res.CompanyBuildingNo = providerRefundRequest.ProviderBid.Company?.BuildingNo.Value.ToString() ?? "-";
            res.CompanyTaxNumber = providerRefundRequest.ProviderBid.Company?.TaxRecordNumber ?? providerRefundRequest.ProviderBid.Freelancer?.FreelanceDocumentNumber ?? "-";
            res.PillNumber = string.IsNullOrEmpty(providerRefundRequest.TransactionNumber) ? "-" : providerRefundRequest.TransactionNumber;
            res.CompanyAdditionalInfo = "-";
            res.CompanyAdditionalAddressNo = "-";
            res.Bid_Ref_Number = providerRefundRequest.ProviderBid.Bid.Ref_Number;

            res.CreationDate = providerRefundRequest.CreationDate;

            res.TenderDocsAmountWithoutTax = (providerRefundRequest.ProviderBid.Price - (providerRefundRequest.ProviderBid.TanafosTaxesAmount + providerRefundRequest.ProviderBid.AssociationTaxesAmount));
            res.TenderDocsAmountWithoutTaxFormatted = res.TenderDocsAmountWithoutTax.ToTwoDigitsAfterDecimalPointWithThousandSeperator();

            res.TenderDocsAmountWithTax = providerRefundRequest.ProviderBid.Price;
            res.TenderDocsAmountWithTaxFormatted = res.TenderDocsAmountWithTax.ToTwoDigitsAfterDecimalPointWithThousandSeperator();

            res.TaxAmount = (providerRefundRequest.ProviderBid.TanafosTaxesAmount + providerRefundRequest.ProviderBid.AssociationTaxesAmount);
            res.TaxAmountFormatted = res.TaxAmount.ToTwoDigitsAfterDecimalPointWithThousandSeperator();
            res.TaxPercentage = ((res.TaxAmount * 100) / res.TenderDocsAmountWithoutTax).ToTwoDigitsAfterDecimalPointWithThousandSeperator();
            res.RefundedAmountToAddon = providerRefundRequest.ProviderBid?.AddOnUsagesHistory?.DiscountQuantity??0;
            res.ContactUsMobile = generalSettings.ContactUsMobile;
            res.TanafosTaxNumber = generalSettings.TanafosTaxNumber;
            res.TanafosCompanyFullName = generalSettings.TanafosCompanyFullName;
            res.TanafosCommericalRecordNo = generalSettings.TanafosCommericalRecordNo;
            res.TanafosBuildingNo = generalSettings.TanafosBuildingNo;
            res.TanafosStreetName = generalSettings.TanafosStreetName;
            res.TanafosNeighborhoodName = generalSettings.TanafosNeighborhoodName;
            res.TanafosCityName = generalSettings.TanafosCityName;
            res.TanafosCountryName = generalSettings.TanafosCountryName;
            res.TanafosPostalCode = generalSettings.TanafosPostalCode;
            res.TanafosAdditionalAddressNo = generalSettings.TanafosAdditionalAddressNo;
            res.TanafosAdditionalInfo = generalSettings.TanafosAdditionalInfo;
            res.Logo_Image = await _imageService.GetImage("Resources/staging-emails-templates/EmailTemplate/billlogo.png");
            res.Saudi_Riyal = await _helperService.GetSaudiRiyalBase64Svg("#333");

            
            return OperationResult<GetProviderDataOfRefundableCompanyBidModel>.Success(res);
        }

        public async Task<OperationResult<bool>> TakeActionOnBidSubmissionBySupervisingBid(BidSupervisingActionRequest req)
        {
            try
            {
                var user = _currentUserService.CurrentUser;
                if (user is null || user.UserType != UserType.Donor)
                    return OperationResult<bool>.Fail(HttpErrorCode.NotAuthorized, CommonErrorCodes.NotAuthorized);

                if ((req.Action != SponsorSupervisingStatus.Approved && req.Action != SponsorSupervisingStatus.Rejected) || (req.Action == SponsorSupervisingStatus.Rejected && string.IsNullOrEmpty(req.RejectionReason)))
                    return OperationResult<bool>.Fail(HttpErrorCode.BusinessRuleViolation, CommonErrorCodes.INVALID_INPUT);

                var bid = await _bidRepository.Find(x => x.Id == req.BidId && !x.IsDeleted
                && x.BidStatusId == (int)TenderStatus.Pending
                )
                    .Include(x => x.BidSupervisingData)
                    .Include(x => x.BidAddressesTime)
                    .Include(x => x.Association)
                    .Include(x => x.Donor)
                    .Include(x => x.Bid_Industries).ThenInclude(x => x.CommercialSectorsTree)
                    .Include(x => x.FreelanceBidIndustries).ThenInclude(x => x.FreelanceWorkingSector.Parent)
                    .FirstOrDefaultAsync();


                if (bid is null)
                    return OperationResult<bool>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);
                var oldStatus = (TenderStatus)bid.BidStatusId;
                var bidSupervisingData = bid.BidSupervisingData.Where(x => x.SupervisingServiceClaimCode == SupervisingServiceClaimCodes.clm_3057).OrderByDescending(x => x.CreationDate).FirstOrDefault();
                var bidDonor = await _donorService.GetBidDonorOfBidIfFound(bid.Id);

                if (bidDonor is null || bidDonor.DonorId != user.CurrentOrgnizationId)
                    return OperationResult<bool>.Fail(HttpErrorCode.NotAuthorized, CommonErrorCodes.NotAuthorized);

                if (!bid.IsFunded || bidSupervisingData.SupervisorStatus != SponsorSupervisingStatus.Pending)
                    return OperationResult<bool>.Fail(HttpErrorCode.InvalidInput, CommonErrorCodes.INVALID_INPUT);


                if (req.Action == SponsorSupervisingStatus.Approved)
                {
                    bid.ModificationDate = _dateTimeZone.CurrentDate;
                    bid.ModifiedBy = user.Id;

                    if (bid.BidTypeId != (int)BidTypes.Instant && bid.BidAddressesTime.LastDateInReceivingEnquiries.HasValue && bid.BidAddressesTime.LastDateInReceivingEnquiries.Value.Date < _dateTimeZone.CurrentDate.Date)
                    {
                        bid.BidStatusId = (int)TenderStatus.Draft;
                        bidSupervisingData.SupervisorStatus = null;

                        await SendEmailToAssociationWhenBidInquiryDateEndAndBidStatusIsPending(bid, bidDonor.Donor);
                        await SendNotificationToAssociationWhenBidInquiryDateEndAndBidStatusIsPending(bid, bidDonor.Donor);
                    }
                    else
                    {
                        bid.BidStatusId = (int)TenderStatus.Reviewing;
                        // bid.CreationDate = _dateTimeZone.CurrentDate;
                        // bid.CreatedBy = user.Id;
                        bidSupervisingData.SupervisorStatus = SponsorSupervisingStatus.Approved;

                        await SendPublishBidRequestEmailAndNotification(user, bid, oldStatus);

                    }

                    await _bidSupervisingDataRepository.UpdateRange(bid.BidSupervisingData.ToList());
                    await _bidRepository.Update(bid);

                    var res = await TakeActionOnBidByDonor(bidDonor.Id, DonorResponse.Accept);
                }
                else
                {
                    bid.BidStatusId = (int)TenderStatus.Draft;
                    bidSupervisingData.SupervisorStatus = SponsorSupervisingStatus.Rejected;
                    bidSupervisingData.RejectionReason = req.RejectionReason;
                    bid.ModificationDate = _dateTimeZone.CurrentDate;
                    bid.ModifiedBy = user.Id;

                    await _bidSupervisingDataRepository.UpdateRange(bid.BidSupervisingData.ToList());
                    await _bidRepository.Update(bid);

                    var res = await TakeActionOnBidByDonor(bidDonor.Id, DonorResponse.Reject);

                    await SendEmailToAssociationWhenDonorRejectBidSubmission(bid, bidDonor.Donor, req.RejectionReason);
                    await SendNotificationToAssociationWhenDonorRejectBidSubmission(bid, bidDonor.Donor, req.RejectionReason);
                }

                return OperationResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = req,
                    ErrorMessage = "Failed to Take Action On Bid Submission By Supervising Bid !",
                    ControllerAndAction = "BidController/TakeActionOnBidSubmissionBySupervisingBid"
                });
                return OperationResult<bool>.Fail(HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }

        }

        private async Task ApproveBidBySupervisor(ApplicationUser user, Bid bid, BidDonor bidDonor)
        {
            if (bid.BidTypeId == (int)BidTypes.Private)
            {
                var entityName = bid.EntityType == UserType.Association ? bid.Association?.Association_Name : bid.Donor?.DonorName;

                var bidInvitation = await _bidInvitationsRepository
                    .Find(a => a.BidId == bid.Id && a.InvitationStatus == InvitationStatus.New)
                    .Include(a => a.Company)
                        .ThenInclude(a => a.Provider)
                    .ToListAsync();
                if (bidInvitation.Any())
                {
                    var _commonEmailAndNotificationService = (ICommonEmailAndNotificationService)_serviceProvider.GetService(typeof(ICommonEmailAndNotificationService));
                    await _commonEmailAndNotificationService.SendInvitationsAfterApproveBid(bidInvitation.ToList(), bid, user, entityName);
                }

                //=================update on list to sent ===========================================
                bidInvitation.ToList().ForEach(a =>
                {
                    a.CreationDate = _dateTimeZone.CurrentDate;
                    a.InvitationStatus = InvitationStatus.Sent;
                    a.ModificationDate = _dateTimeZone.CurrentDate;
                    a.ModifiedBy = user.Id;
                    a.Company = null;
                });
                await _bidInvitationsRepository.UpdateRange(bidInvitation.ToList());
            }
            if (bid.TenderBrochurePoliciesType == TenderBrochurePoliciesType.UsingRFP)
                await SaveRFPAsPdf(bid);

            await SendEmailToAssociationWhenDonorApproveBidSubmission(bid, bidDonor.Donor);
            await SendNotificationToAssociationWhenDonorApproveBidSubmission(bid, bidDonor.Donor);


            await DoBusinessAfterPublishingBid(bid, user);
            await _bidRepository.ExexuteAsTransaction(async () =>
            {
                await _pointEventService.AddPointEventUsageHistoryAsync(new AddPointEventUsageHistoryModel
                {
                    PointType = PointTypes.PublishNonDraftBid,
                    ActionId = bid.Id,
                    EntityId = bid.AssociationId.HasValue ? bid.AssociationId.Value : bid.DonorId.Value,
                    EntityUserType = bid.AssociationId.HasValue ? UserType.Association : UserType.Donor,
                });

                await LogBidCreationEvent(bid);
                await _helperService.AddReviewedSystemRequestLog(new AddReviewedSystemRequestLogRequest()
                {
                    EntityId = bid.Id,
                    RejectionReason = null,
                    SystemRequestStatus = SystemRequestStatuses.Accepted,
                    SystemRequestType = SystemRequestTypes.BidReviewing,

                }, user);
                await _bidRepository.Update(bid);
            });
            string OTPMessage = $"{bid.BidName} تم نشر منافستكم";

            var res = await _sMSService.SendAsync(OTPMessage, bid.Association.Manager_Mobile, (int)SystemEventsTypes.ApproveBidOTP, UserType.Association);
            // handle for freelancer
            await SendSMSPublishBidToProvider(bid);

        }

        private async Task DoBusinessAfterPublishingBid(Bid bid, ApplicationUser usr)
        {
            var _commonEmailAndNotificationService = (ICommonEmailAndNotificationService)_serviceProvider.GetService(typeof(ICommonEmailAndNotificationService));

            if (usr.UserType == UserType.SuperAdmin || usr.UserType == UserType.Admin)
                await _commonEmailAndNotificationService.SendEmailBySuperAdminToTheCreatorOfBidAfterBidPublished(bid);

            await _commonEmailAndNotificationService.SendEmailAndNotifyToInvitedAssociationByDonorIfFound(bid);

            await SendEmailAndNotifyDonor(bid);
            await SendNewBidEmailToSuperAdmins(bid);
        }

        private async Task SendEmailToAssociationWhenDonorApproveBidSubmission(Bid bid, Donor donor)
        {
            if ((bid is null || donor is null) || !bid.AssociationId.HasValue)
                return;

            var bidIndustriesNames = await _bidIndustryRepository
           .Find(x => x.BidId == bid.Id)
           .Select(i => i.CommercialSectorsTree.NameAr)
           .ToListAsync();

            var bidIndustriesAsString = string.Join(" ،", bidIndustriesNames);
            var emailModel = new ApproveBidBySupervisingDonorEmail()
            {
                BaseBidEmailDto = await _helperService.GetBaseDataForBidsEmails(bid),
                Industries = bidIndustriesAsString,

                LastDateInOfferSubmissionAndTime = bid.BidAddressesTime is null ? string.Empty :
               bid.BidAddressesTime.LastDateInOffersSubmission.HasValue ?
                   bid.BidAddressesTime.LastDateInOffersSubmission.Value.ToArabicFormatWithTime() :
                   string.Empty,
                DonorName = donor.DonorName
            };
            var emailRequest = new EmailRequest
            {
                ControllerName = BaseBidEmailDto.BidsEmailsPath,
                ViewName = ApproveBidBySupervisingDonorEmail.EmailTemplateName,
                ViewObject = emailModel,
                To = await _associationService.GetEmailToSend(bid.AssociationId.Value, bid.Association.Manager_Email),
                Subject = $"تم اعتماد منافستكم {bid.BidName} بواسطة {donor.DonorName}",
                SystemEventType = (int)SystemEventsTypes.ApproveBidBySupervisingDonorEmail,
            };

            await _emailService.SendAsync(emailRequest);
        }
        private async Task SendNotificationToAssociationWhenDonorApproveBidSubmission(Bid bid, Donor donor)
        {
            if ((bid is null || donor is null) || !bid.AssociationId.HasValue)
                return;

            var recievers = await _notificationUserClaim.GetUsersClaim(new string[] { AssociationClaimCodes.clm_3030.ToString() }, bid.AssociationId.Value, OrganizationType.Assosition);

            if (recievers.ActualReceivers.Count <= 0)
                return;

            var _notificationService = (INotificationService)_serviceProvider.GetService(typeof(INotificationService));
            var notificationObj = await _notificationService.GetNotificationObjModelToSendAsync(new NotificationObjModel
            {
                SenderId = _currentUserService.CurrentUser?.Id,
                EntityId = bid.Id,
                NotificationType = NotificationType.SupervisingDonorApproveBidSubmission,
                Message = $"تم اعتماد منافستكم {bid.BidName} بواسطة {donor.DonorName}",
                ActualRecieverIds = recievers.ActualReceivers,
                ServiceType = ServiceType.Bids,
                SystemEventType = (int)SystemEventsTypes.ApproveBidNotification

            });

            notificationObj.SenderName = donor.DonorName;
            notificationObj.BidName = bid.BidName;
            notificationObj.BidId = bid.Id;

            await _notificationService.SendRealTimeNotificationToUsersAsync(notificationObj, recievers.RealtimeReceivers.Select(a => a.ActualRecieverId).ToList(), (int)SystemEventsTypes.ApproveBidNotification);
        }

        private async Task SendEmailToAssociationWhenDonorRejectBidSubmission(Bid bid, Donor donor, string rejectionReason)
        {
            if ((bid is null || donor is null) || !bid.AssociationId.HasValue)
                return;


            var emailModel = new RejectBidBySupervisingDonorEmail()
            {
                BaseBidEmailDto = await _helperService.GetBaseDataForBidsEmails(bid),
                RejectReason = rejectionReason,
                DonorName = donor.DonorName
            };
            var emailRequest = new EmailRequest
            {
                ControllerName = BaseBidEmailDto.BidsEmailsPath,
                ViewName = RejectBidBySupervisingDonorEmail.EmailTemplateName,
                ViewObject = emailModel,
                To = await _associationService.GetEmailToSend(bid.AssociationId.Value, bid.Association.Manager_Email),
                Subject = $"تم رفض منافستكم {bid.BidName} بواسطة {donor.DonorName}",
                SystemEventType = (int)SystemEventsTypes.RejectBidBySupervisingDonorEmail
            };
            await _emailService.SendAsync(emailRequest);
        }
        private async Task SendNotificationToAssociationWhenDonorRejectBidSubmission(Bid bid, Donor donor, string rejectionReason)
        {
            if ((bid is null || donor is null) || !bid.AssociationId.HasValue)
                return;

            var recievers = await _notificationUserClaim.GetUsersClaim(new string[] { AssociationClaimCodes.clm_3030.ToString() }, bid.AssociationId.Value, OrganizationType.Assosition);

            if (recievers.ActualReceivers.Count <= 0)
                return;

            var _notificationService = (INotificationService)_serviceProvider.GetService(typeof(INotificationService));
            var notificationObj = await _notificationService.GetNotificationObjModelToSendAsync(new NotificationObjModel
            {
                SenderId = _currentUserService.CurrentUser?.Id,
                EntityId = bid.Id,
                NotificationType = NotificationType.SupervisingDonorRejectBidSubmission,
                Message = $"تم رفض منافستكم {bid.BidName} بواسطة {donor.DonorName}",
                ActualRecieverIds = recievers.ActualReceivers,
                ServiceType = ServiceType.Bids
                ,
                SystemEventType = (int)SystemEventsTypes.RejectBidNotification

            });

            notificationObj.SenderName = donor.DonorName;
            notificationObj.AssociationName = donor.DonorName;
            notificationObj.BidName = bid.BidName;
            notificationObj.BidId = bid.Id;

            await _notificationService.SendRealTimeNotificationToUsersAsync(notificationObj, recievers.RealtimeReceivers.Select(a => a.ActualRecieverId).ToList(), (int)SystemEventsTypes.RejectBidNotification);

        }

        private async Task SendEmailToAssociationWhenBidInquiryDateEndAndBidStatusIsPending(Bid bid, Donor donor)
        {
            if ((bid is null || donor is null) || !bid.AssociationId.HasValue)
                return;

            var emailModel = new ApproveBidPeriodEndedEmail()
            {
                BaseBidEmailDto = await _helperService.GetBaseDataForBidsEmails(bid),
                DonorName = donor.DonorName
            };
            var emailRequest = new EmailRequest
            {
                ControllerName = BaseBidEmailDto.BidsEmailsPath,
                ViewName = ApproveBidPeriodEndedEmail.EmailTemplateName,
                ViewObject = emailModel,
                To = await _associationService.GetEmailToSend(bid.AssociationId.Value, bid.Association.Manager_Email),
                Subject = $"انتهت مهلة اعتماد منافستكم {bid.BidName} من قبل {donor.DonorName}",
                SystemEventType = (int)SystemEventsTypes.ApproveBidPeriodEndedEmail
            };

            await _emailService.SendAsync(emailRequest);
        }
        private async Task SendNotificationToAssociationWhenBidInquiryDateEndAndBidStatusIsPending(Bid bid, Donor donor)
        {
            if ((bid is null || donor is null) || !bid.AssociationId.HasValue)
                return;

            var recievers = await _notificationUserClaim.GetUsersClaim(new string[] { AssociationClaimCodes.clm_3030.ToString() }, bid.AssociationId.Value, OrganizationType.Assosition);

            if (recievers.ActualReceivers.Count <= 0)
                return;

            var _notificationService = (INotificationService)_serviceProvider.GetService(typeof(INotificationService));
            var notificationObj = await _notificationService.GetNotificationObjModelToSendAsync(new NotificationObjModel
            {
                SenderId = _currentUserService.CurrentUser?.Id,
                EntityId = bid.Id,
                NotificationType = NotificationType.SupervisingDonorBidSubmissionDateEnded,
                Message = $"انتهت مهلة اعتماد منافستكم {bid.BidName} من قبل {donor.DonorName}",
                ActualRecieverIds = recievers.ActualReceivers,
                ServiceType = ServiceType.Bids,
                SystemEventType = (int)SystemEventsTypes.DonorBidSubmissionDateEndedNotification

            });

            notificationObj.SenderName = donor.DonorName;
            notificationObj.BidName = bid.BidName;
            notificationObj.BidId = bid.Id;

            await _notificationService.SendRealTimeNotificationToUsersAsync(notificationObj, recievers.RealtimeReceivers.Select(a => a.ActualRecieverId).ToList(), (int)SystemEventsTypes.DonorBidSubmissionDateEndedNotification);
        }

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
                string randomNumber = await GenerateBidRefNumber(bid.Id, firstPart_Ref_Number);

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
                    BidTypeBudgetId = await MapBidTypeBudgetId(bid),
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
                await AddBidRegions(bid.BidRegions.Select(a => a.RegionId).ToList(), copyBid.Id);
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
                await _freelanceBidIndustryRepository.AddRange(FreelanceBidIndustries);
                #endregion

                #region add Bid Donner
                if (bid.IsFunded && bid.BidDonor is not null)
                {
                    BidDonorRequest bidDonorRequest = new BidDonorRequest();
                    bidDonorRequest.DonorId = bid.BidDonor.DonorId.GetValueOrDefault();
                    bidDonorRequest.NewDonorName = bid.BidDonor.NewDonorName;
                    bidDonorRequest.Email = bid.BidDonor.Email;
                    bidDonorRequest.PhoneNumber = bid.BidDonor.PhoneNumber;
                    var res = await SaveBidDonor(bidDonorRequest, copyBid.Id, usr.Id);
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
                        var res = await this.AddInvitationToAssocationByDonorIfFound(invitedAssociationByDonorModel, copyBid, bid.IsAssociationFoundToSupervise, bid.SupervisingAssociationId);
                    }
                }
                #endregion

                #region SendNewDraftBidEmailToSuperAdmins
                var entityName = bid.AssociationId.HasValue ? bid.Association.Association_Name : bid.Donor.DonorName;
                await SendNewDraftBidEmailToSuperAdmins(copyBid, entityName);
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

                await CopyBidAchievementPhasesPhases(bid, copyBid);

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

        private async Task<long?> MapBidTypeBudgetId(Bid bid)
        {
            if (bid.BidTypeBudgetId is null)
                return null;

            var bidTypeBudget = await _bidTypesBudgetsRepository.FindOneAsync(a => a.Id == bid.BidTypeBudgetId.Value, false);
            if (bidTypeBudget is null)
                return null;

            return bidTypeBudget.Id;
        }

        private async Task CopyBidAchievementPhasesPhases(Bid bid, Bid copyBid)
        {
            await _bidAchievementPhasesRepository.AddRange(bid.BidAchievementPhases.Select(oldPhase => new BidAchievementPhases
            {
                BidId = copyBid.Id,
                CreationDate = _dateTimeZone.CurrentDate,
                DeliverDateFrom = oldPhase.DeliverDateFrom,
                DeliverDateTo = oldPhase.DeliverDateTo,
                PercentageValue = oldPhase.PercentageValue,
                PhaseStatus = PhaseStatus.Pending,
                Title = oldPhase.Title,
                BidAchievementPhaseAttachments = oldPhase.BidAchievementPhaseAttachments.Select(oldFile => new BidAchievementPhaseAttachments
                {
                    IsRequired = true,
                    Title = oldFile.Title,
                }).ToList()
            }).ToList());
        }

        public async Task<OperationResult<GetBidsSearchHeadersResponse>> GetBidsSearchHeadersAsync()
        {
            try
            {
                var user = _currentUserService.CurrentUser;

                var res = new GetBidsSearchHeadersResponse();
                res.BidTypes = await _bidTypeRepository.Find(x => x.IsVisible)
                    .ToListAsync();

                res.TermsBookPrices = await _termsBookPriceRepository.Find(x => x.IsVisible)
                    .ToListAsync();

                if (user is null)
                    return OperationResult<GetBidsSearchHeadersResponse>.Success(res);

                var userSearchRsult = await _userSearchService.GetUserSearch();
                if (!userSearchRsult.IsSucceeded)
                    return OperationResult<GetBidsSearchHeadersResponse>.Fail(userSearchRsult.HttpErrorCode, userSearchRsult.Code, userSearchRsult.ErrorMessage);

                res.UserSearches = userSearchRsult.Data;

                return OperationResult<GetBidsSearchHeadersResponse>.Success(res);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = null,
                    ErrorMessage = "Failed to Get Bids Search Headers!",
                    ControllerAndAction = "BidController/BidsSearchHeadersAsync"
                });
                return OperationResult<GetBidsSearchHeadersResponse>.Fail(HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }

        private async Task SendSMSPublishBidToProvider(Bid bid)
        {

            var emails = bid.BidTypeId!=(int) BidTypes.Freelancing?
                await _bidsOfProviderRepository.GetProvidersEmailsOfCompaniesSubscribedToBidIndustries(bid):
                await GetFreelancersWithSameWorkingSectors(_freelancerRepository,bid)
                 ;
             
            //string otpMessage1 = $"تم نشر منافسة جديدة في قطاع عملك {bid.BidName}";
            string otpMessage = $"منصة تنافُس تتشرف بدعوتكم للمشاركة في منافسة {bid.BidName} ، يتم استلام العروض فقط عبر منصة تنافُس ، رابط المنافسة و التفاصيل :  {fileSettings.ONLINE_URL}view-bid-details/{bid.Id}";
            var filteredItems = emails.Select(x => x.Mobile).ToList();
            if (fileSettings.ENVIROMENT_NAME.ToLower() == EnvironmentNames.production.ToString().ToLower())
                filteredItems.Add(fileSettings.SendSMSTo);
            var recipients = string.Join(',', filteredItems);
            await _sMSService.SendAsync(otpMessage, string.Join(",", recipients), (int)SystemEventsTypes.PublishBidOTP, UserType.Provider);

        }

        public async Task<OperationResult<bool>> ToggelsAbleToSubscribeToBid(long bidId)
        {
            try
            {
                var usr = _currentUserService.CurrentUser;
                var authorizedTypes = new List<UserType>() { UserType.SuperAdmin, UserType.Admin };

                if (usr == null || !authorizedTypes.Contains(usr.UserType))
                    return OperationResult<bool>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);

                var bid = await _bidRepository.FindOneAsync(a => a.Id == bidId && a.BidStatusId == (int)TenderStatus.Open);
                if (bid == null)
                    return OperationResult<bool>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.YOU_CAN_NOT_DISABLE_SUBSCRIBE_NOT_OPEND_BID);

                bid.IsAbleToSubscribeToBid = !bid.IsAbleToSubscribeToBid;

                var logEventType = bid.IsAbleToSubscribeToBid ? EventTypes.ToggelsAbleToSubscribeToBidOn : EventTypes.ToggelsAbleToSubscribeToBidOff;
                await LogToggelsAbleToSubscribeToBidAction(bid, logEventType, usr);

                await _bidRepository.Update(bid);

                //============================Response ========================
                return OperationResult<bool>.Success(bid.IsAbleToSubscribeToBid);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = $"bidid = {bidId}",
                    ErrorMessage = "Failed to ToggelsAbleToSubscribeToBid!",
                    ControllerAndAction = "bis/ToggelsAbleToSubscribeToBid"
                });
                return OperationResult<bool>.Fail(
                       HttpErrorCode.ServerError, MarketCommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }

        private async Task LogToggelsAbleToSubscribeToBidAction(Bid bid, EventTypes eventType, ApplicationUser user)
        {
            //===============log event===============
            string[] styles = await _helperService.GetEventStyle(eventType);
            await _helperService.LogBidEvents(new BidEventModel
            {
                BidId = bid.Id,
                BidStatus = (TenderStatus)bid.BidStatusId,
                BidEventSection = BidEventSections.Bid,
                BidEventTypeId = (int)eventType,
                EventCreationDate = _dateTimeZone.CurrentDate,
                ActionId = bid.Id,
                Audience = AudienceTypes.Admins,
                CompanyId = 0,
                Header = string.Format(styles[0], user.Name, _dateTimeZone.CurrentDate.ToString("dddd d MMMM، yyyy , h:mm tt", new CultureInfo("ar-AE"))),
            });
        }

        public (decimal, decimal) GetTanafosAssociationFeesOfBoughtTermsBooks(IEnumerable<ProviderBid> pbs)
        {
            decimal totalAssociationFees = 0;
            decimal totalTanafosFees = 0;

            foreach (var pb in pbs)
            {
                var (associationFees, tanafosFees) = GetTanafosAssociationFeesOfBoughtTermsBook(pb);

                totalAssociationFees += associationFees;
                totalTanafosFees += tanafosFees;
            }

            return (totalAssociationFees, totalTanafosFees);
        }

        public (decimal, decimal) GetTanafosAssociationFeesOfBoughtTermsBook(ProviderBid pb)
        {
            double associationFees = 0;
            double tanafosFees = 0;

            if (string.IsNullOrEmpty(pb.CouponHash))
            {
                associationFees += pb.Bid.Association_Fees;
                tanafosFees += pb.Bid.Tanafos_Fees;
            }
            else
            {
                associationFees += pb.AssociationFeesAfterDiscount;
                tanafosFees += pb.TanafosFeesAfterDiscount;
            }

            associationFees += pb.AssociationTaxesAmount;
            tanafosFees += pb.TanafosTaxesAmount;

            return ((decimal)associationFees, (decimal)tanafosFees);
        }

        private async Task UpdateBidRelatedAttachmentsFileNameAfterBidNameChanging(long bidId, string newBidName)
        {
            var bid = await _bidRepository.Find(x => x.Id == bidId, true, false)
                .Include(x => x.Association)
                .Include(x => x.Donor)
                .Include(x => x.BidAttachment)
                .Include(x => x.BidAnnouncements)
                .AsSplitQuery()
                .FirstOrDefaultAsync(); // bidCreator Upload


            if (bid is null)
                throw new Exception($"Bid With the Id {bidId} Hasn't Found");

            var bidOwner = bid.GetBidCreatorName();

            var quotations = await _tenderSubmitQuotationRepository.Find(x => x.BidId == bid.Id, true, false)
                .Include(x => x.Company)
                .Include(x => x.TenderQuotationAttachments)
                .ToListAsync(); // company upload

            var contracts = await _contractRepository.Find(x => x.TenderId == bidId, true, false)
                .ToListAsync(); // association upload

            var contractsId = contracts.Select(x => x.Id).ToList();
            var financialRequests = await _financialRequestRepository.Find(x => contractsId.Contains(x.ContractId), true, false)
                .Include(x => x.Company)
                .Include(x => x.ProviderAchievementPhaseAttachments)
                .ThenInclude(x => x.Company)
                .ToListAsync();

            var namingReq = new UploadFilesRequest { AttachmentNameCategory = AttachmentNameCategories.TermsBookAttachment };

            if (!string.IsNullOrEmpty(bid.Tender_Brochure_Policies_Url))
            {
                var termsBookExtension = Path.GetExtension(bid.Tender_Brochure_Policies_Url);
                bid.Tender_Brochure_Policies_FileName = await _imageService.GetConvientFileName(namingReq, termsBookExtension, newBidName, bidOwner, bid.Ref_Number);
            }

            namingReq.AttachmentNameCategory = AttachmentNameCategories.SupportAttachment;
            foreach (var bidAttachment in bid.BidAttachment)
            {
                if (string.IsNullOrEmpty(bidAttachment.AttachedFileURL))
                    continue;
                var extension = Path.GetExtension(bidAttachment.AttachedFileURL);

                bidAttachment.AttachmentName = await _imageService.GetConvientFileName(namingReq, extension, newBidName, bidOwner, bid.Ref_Number);
            }

            namingReq.AttachmentNameCategory = AttachmentNameCategories.AnnouncementAttachment;
            foreach (var announcment in bid.BidAnnouncements)
            {
                if (string.IsNullOrEmpty(announcment.AttachmentUrl))
                    continue;
                var extension = Path.GetExtension(announcment.AttachmentUrl);

                announcment.AttachmentUrlFileName = await _imageService.GetConvientFileName(namingReq, extension, newBidName, bidOwner, bid.Ref_Number);
            }

            foreach (var quot in quotations)
            {
                foreach (var quotAttach in quot.TenderQuotationAttachments)
                {
                    if (string.IsNullOrEmpty(quotAttach.FileUrl))
                        continue;
                    if (quotAttach.QuotationAttachmentType == QuotationAttachmentTypes.FinancialUploader)
                        namingReq.AttachmentNameCategory = AttachmentNameCategories.QuotationFinancialAttachment;
                    else if (quotAttach.QuotationAttachmentType == QuotationAttachmentTypes.TechnicalUploader)
                        namingReq.AttachmentNameCategory = AttachmentNameCategories.QuotationTechnicalAttachment;
                    else if (quotAttach.QuotationAttachmentType == QuotationAttachmentTypes.All)
                        namingReq.AttachmentNameCategory = AttachmentNameCategories.QuotationTechnicalAndFinancialAttachment;

                    var extension = Path.GetExtension(quotAttach.FileUrl);
                    quotAttach.FileName = await _imageService.GetConvientFileName(namingReq, extension, quot.Company.CompanyName, bidOwner, bid.Ref_Number);
                }
            }

            foreach (var contract in contracts)
            {
                if (!string.IsNullOrEmpty(contract.ContractFileUrl))
                {
                    var contractExtension = Path.GetExtension(contract.ContractFileUrl);

                    namingReq.AttachmentNameCategory = AttachmentNameCategories.ContractAttachment;
                    contract.ContractFileUrl = await _imageService.GetConvientFileName(namingReq, contractExtension, newBidName, bidOwner, bid.Ref_Number);
                }

                if (!string.IsNullOrEmpty(contract.AwardingLetterFileUrl))
                {
                    var contractAwardingLetterExtension = Path.GetExtension(contract.AwardingLetterFileUrl);

                    namingReq.AttachmentNameCategory = AttachmentNameCategories.ContractAwardingLetterAttachment;
                    contract.AwardingLetterFileUrl = await _imageService.GetConvientFileName(namingReq, contractAwardingLetterExtension, newBidName, bidOwner, bid.Ref_Number);
                }
            }

            foreach (var finReq in financialRequests)
            {
                if (!string.IsNullOrEmpty(finReq.InvoiceURL))
                {
                    var invoiceExtension = Path.GetExtension(finReq.InvoiceURL);
                    namingReq.AttachmentNameCategory = AttachmentNameCategories.FinancialRequestInvoiceAttachment;
                    finReq.InvoiceURLFileName = await _imageService.GetConvientFileName(namingReq, invoiceExtension, newBidName, finReq.Company.CompanyName, finReq.FinancialRequestNumber);
                }

                if (!string.IsNullOrEmpty(finReq.TransferNumberAttachementUrl))
                {
                    var transerExtension = Path.GetExtension(finReq.TransferNumberAttachementUrl);
                    namingReq.AttachmentNameCategory = AttachmentNameCategories.FinancialRequestInvoiceBankTransferPaymentAttachment;
                    finReq.TransferNumberAttachementUrlFileName = await _imageService.GetConvientFileName(namingReq, transerExtension, newBidName, bidOwner, finReq.FinancialRequestNumber);
                }

                foreach (var attach in finReq.ProviderAchievementPhaseAttachments)
                {
                    if (string.IsNullOrEmpty(attach.FilePath))
                        continue;

                    var extension = Path.GetExtension(attach.FilePath);
                    namingReq.AttachmentNameCategory = AttachmentNameCategories.AchievementPhaseAttachment;
                    finReq.TransferNumberAttachementUrlFileName = await _imageService.GetConvientFileName(namingReq, extension, newBidName, finReq.Company.CompanyName, bid.Ref_Number);
                }
            }


            await _bidRepository.ExexuteAsTransaction(async () =>
            {
                await _bidRepository.Update(bid);
                await _tenderSubmitQuotationRepository.UpdateRange(quotations);
                await _contractRepository.UpdateRange(contracts);
                await _financialRequestRepository.UpdateRange(financialRequests);
            });
        }


        //3
        public async Task<OperationResult<List<GetCompaniesToBuyTermsBookResponse>>> GetCurrentUserCompaniesToBuyTermsBookWithForbiddenReasonsIfFoundAsync(long bidId, long? currenctUserSpecificCompanyId = null)
        {
            try
            {
                var user = _currentUserService.CurrentUser;
                if (_currentUserService.IsUserNotAuthorized(new List<UserType> { UserType.Provider }))
                    return OperationResult<List<GetCompaniesToBuyTermsBookResponse>>.Fail(HttpErrorCode.NotAuthorized, CommonErrorCodes.NotAuthorized);

                var bid = await _bidRepository.Find(x => x.Id == bidId
                && x.BidStatusId == (int)TenderStatus.Open)
                    .Include(x => x.ProviderBids.Where(x => x.IsPaymentConfirmed))
                    .Include(x => x.BidInvitations)
                    .FirstOrDefaultAsync();

                if (bid is null)
                    return OperationResult<List<GetCompaniesToBuyTermsBookResponse>>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);

                var userAssignedCompanOrgs = await _organizationUserRepository.Find(x => x.UserId == user.Id && x.Organization.OrgTypeID == OrganizationType.Comapny && !x.Organization.IsSusPend)
                    .WhereIf(currenctUserSpecificCompanyId.HasValue, userOrg => userOrg.Organization.EntityID == currenctUserSpecificCompanyId.Value)
                    .Include(x => x.Organization)
                    .Include(x => x.ServiceClaimRoles)
                    .ThenInclude(x => x.ServiceClaim)
                    .AsSplitQuery()
                    .ToListAsync();

                var companyIds = userAssignedCompanOrgs.Select(x => x.Organization.EntityID);
                var companies = await _companyRepository.Find(x => companyIds.Contains(x.Id) && x.CompanyRegistrationStatus != RegistrationStatus.NotReviewed && x.CompanyRegistrationStatus != RegistrationStatus.Rejected)
                    .ToListAsync();

                var res = new List<GetCompaniesToBuyTermsBookResponse>();
                foreach (var company in companies)
                {
                    var userCompanyOrg = userAssignedCompanOrgs.FirstOrDefault(x => x.Organization.EntityID == company.Id);
                    var obj = new GetCompaniesToBuyTermsBookResponse();

                    obj.Id = company.Id;
                    obj.Name = company.CompanyName;

                    if (bid.ProviderBids.Any(x => x.CompanyId == company.Id))
                        obj.BuyTermsBookForbiddenReasons.Add(BuyTermsBookForbiddenReasons.CompanyBoughtTermsBookBefore);


                    if (bid.BidTypeId == (int)BidTypes.Private && !bid.BidInvitations.Any(x => x.CompanyId == company.Id))
                        obj.BuyTermsBookForbiddenReasons.Add(BuyTermsBookForbiddenReasons.CompanyNotInvitedInLimitedBid);


                    if (company.EstablishmentStatusId != (int)EstablishmentStatus.Active)
                        obj.BuyTermsBookForbiddenReasons.Add(BuyTermsBookForbiddenReasons.CompanyNotActive);

                    if (string.IsNullOrEmpty(company.UniqueNumber700))
                        obj.BuyTermsBookForbiddenReasons.Add(BuyTermsBookForbiddenReasons.CompanyUniqueNumber700NotProvided);


                    if (string.IsNullOrEmpty(company.DelegationFile))
                        obj.BuyTermsBookForbiddenReasons.Add(BuyTermsBookForbiddenReasons.CompanyDelegationFileNotUploaded);


                    if (company.CompanyRegistrationStatus == RegistrationStatus.NotReviewed || company.CompanyRegistrationStatus == RegistrationStatus.Expire)
                        obj.BuyTermsBookForbiddenReasons.Add(BuyTermsBookForbiddenReasons.CompanyIsNotSubscribedInSystem);


                    if (userCompanyOrg.ServiceClaimRoles.All(x => x.ServiceClaim.ClaimCode != ProviderClaimCodes.clm_3039.ToString()))
                        obj.BuyTermsBookForbiddenReasons.Add(BuyTermsBookForbiddenReasons.UserNotHaveBuyTermsBookPermission);

                    if (bid.IsBidAssignedForAssociationsOnly && company.AssignedAssociationId is null && company.AssignedDonorId is null)
                        obj.BuyTermsBookForbiddenReasons.Add(BuyTermsBookForbiddenReasons.CompanyIsNotAssignedByNonProfitEntity);


                    obj.IsAbleToBuy = obj.BuyTermsBookForbiddenReasons.Count == 0;
                    res.Add(obj);
                }

                return OperationResult<List<GetCompaniesToBuyTermsBookResponse>>.Success(res);

            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = $"bidid = {bidId}",
                    ErrorMessage = "Failed to Get Companies To Buy Terms Book Async!",
                    ControllerAndAction = "bid/CompaniesToBuyTermsBookAsync"
                });
                return OperationResult<List<GetCompaniesToBuyTermsBookResponse>>.Fail(HttpErrorCode.ServerError, MarketCommonErrorCodes.OPERATION_FAILED, refNo);
            }

        }

        public async Task<OperationResult<GetFreelancersToBuyTermsBookResponse>> GetCurrentUserFreelancersToBuyTermsBookWithForbiddenReasonsIfFoundAsync(long bidId, long? freelancerId)
        {
            try
            {
                var user = _currentUserService.CurrentUser;
                if (_currentUserService.IsUserNotAuthorized(new List<UserType> { UserType.Freelancer }))
                    return OperationResult<GetFreelancersToBuyTermsBookResponse>.Fail(HttpErrorCode.NotAuthorized, CommonErrorCodes.NotAuthorized);

                var bid = await _bidRepository.Find(x => x.Id == bidId
                && x.BidStatusId == (int)TenderStatus.Open)
                    .Include(x => x.ProviderBids.Where(x => x.IsPaymentConfirmed))
                    .Include(x => x.BidInvitations)
                    .FirstOrDefaultAsync();

                if (bid is null)
                    return OperationResult<GetFreelancersToBuyTermsBookResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);

                var freelancer = await _freelancerRepository.Find(x => x.Id==freelancerId && x.RegistrationStatus != RegistrationStatus.NotReviewed && x.RegistrationStatus != RegistrationStatus.Rejected)
                    .FirstOrDefaultAsync();

                if (freelancer is null)
                    return OperationResult<GetFreelancersToBuyTermsBookResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.FREELANCER_NOT_FOUND);

                var obj = new GetFreelancersToBuyTermsBookResponse();

                    obj.Id = freelancer.Id;
                    obj.Name = freelancer.Name;

                    if (bid.ProviderBids.Any(x => x.FreelancerId == freelancer.Id))
                        obj.BuyTermsBookForbiddenReasons.Add(FreelancerBuyTermsBookForbiddenReasons.FreelancerBoughtTermsBookBefore);

                    if (freelancer.FreelanceDocumentExpirationDate < _dateTimeZone.CurrentDate)
                        obj.BuyTermsBookForbiddenReasons.Add(FreelancerBuyTermsBookForbiddenReasons.FreelancerRegisterationExpiryDateIsExpired);

                    if (freelancer.RegistrationStatus == RegistrationStatus.NotReviewed || freelancer.RegistrationStatus == RegistrationStatus.Expire)
                        obj.BuyTermsBookForbiddenReasons.Add(FreelancerBuyTermsBookForbiddenReasons.FreelancerIsNotSubscribedInSystem);


                var freelancerOrg =await _organizationUserRepository.Find(x => x.Organization.EntityID == freelancer.Id && x.Organization.OrgTypeID == OrganizationType.Freelancer)
                                                                    .Include(x=>x.ServiceClaimRoles).ThenInclude(x=>x.ServiceClaim).FirstOrDefaultAsync();

                if (freelancerOrg is null)
                    return OperationResult<GetFreelancersToBuyTermsBookResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.INVALID_ORGANIZATION);

                if (freelancerOrg.ServiceClaimRoles.All(x => x.ServiceClaim.ClaimCode != FreelancerClaimCodes.clm_8001.ToString()))
                        obj.BuyTermsBookForbiddenReasons.Add(FreelancerBuyTermsBookForbiddenReasons.UserNotHaveBuyTermsBookPermission);

                    obj.IsAbleToBuy = obj.BuyTermsBookForbiddenReasons.Count == 0;
                   
               return OperationResult<GetFreelancersToBuyTermsBookResponse>.Success(obj);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = $"bidid = {bidId}",
                    ErrorMessage = "Failed to Get Freelancers To Buy Terms Book Async!",
                    ControllerAndAction = "bid/FreelancersToBuyTermsBookAsync"
                });
                return OperationResult<GetFreelancersToBuyTermsBookResponse>.Fail(HttpErrorCode.ServerError, MarketCommonErrorCodes.OPERATION_FAILED, refNo);
            }

        }

        private async Task<(bool IsSuceeded, string ErrorMessage, string LogRef, long Count)> SendEmailToCompaniesLimitedOffersChanged(Bid bid)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var helperService = scope.ServiceProvider.GetRequiredService<IHelperService>();
            var convertViewService = scope.ServiceProvider.GetRequiredService<IConvertViewService>();
            var bidsOfProviderRepository = scope.ServiceProvider.GetRequiredService<ITenderSubmitQuotationRepositoryAsync>();
            var sendinblueService = scope.ServiceProvider.GetRequiredService<ISendinblueService>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var emailSettingService = scope.ServiceProvider.GetRequiredService<IEmailSettingService>();
            var sMSService = scope.ServiceProvider.GetRequiredService<ISMSService>();

            var providersEmails = await bidsOfProviderRepository.GetProvidersEmailsOfCompaniesSubscribedToBidIndustries(bid);

            var bidTermsBookBuyers = await _helperService.GetBidTermsBookBuyersDataAsync(bid);

            /*providersEmails = providersEmails.Where(provider => !providerBuyTrems.Contains(provider.Email))
               .ToList();*/

            string subject = $"تم تمديد فرصة استقبال العروض للمنافسة {bid.BidName}";
            var model = new BidLimitedOffersChangedEmail()
            {
                BaseBidEmailDto = await helperService.GetBaseDataForBidsEmails(bid)
            };
            model.LimitedOffers = bid.limitedOffers ?? 0;
            model.CurrentOffersount = await _tenderSubmitQuotationRepository.Find(x => x.BidId == bid.Id && x.ProposalStatus == ProposalStatus.Delivered, false)
                .CountAsync();

            model.BidName = bid.BidName;
            var html = await convertViewService.RenderViewAsync(BaseBidEmailDto.BidsEmailsPath, BidLimitedOffersChangedEmail.EmailTemplateName, model);
            var currentEmailSettingId = (await emailSettingService.GetActiveEmailSetting()).Data;

            if (fileSettings.ENVIROMENT_NAME.ToLower() == EnvironmentNames.production.ToString().ToLower()
                && currentEmailSettingId == (int)EmailSettingTypes.SendinBlue)
            {
                try
                {
                    var createdListId = await sendinblueService.CreateListOfContacts($"موردين قطاعات المنافسة ({bid.Ref_Number})", _sendinblueOptions.FolderId);
                    await sendinblueService.ImportContactsInList(new List<long?> { createdListId }, providersEmails);

                    var createCampaignModel = new CreateCampaignModel
                    {
                        HtmlContent = html,
                        AttachmentUrl = null,
                        CampaignSubject = subject,
                        ListIds = new List<long?> { createdListId },
                        ScheduledAtDate = null
                    };
                    var campaignResponse = await sendinblueService.CreateEmailCampaign(createCampaignModel);
                    if (!campaignResponse.IsSuccess)
                        return (campaignResponse.IsSuccess, campaignResponse.ErrorMessage, campaignResponse.LogRef, 0);

                    await sendinblueService.SendEmailCampaignImmediately(campaignResponse.Id);
                }
                catch (Exception ex)
                {
                    await _helperService.AddBccEmailTracker(new EmailRequestMultipleRecipients
                    {
                        Body = html,
                        Attachments = null,
                        Recipients = providersEmails.Select(x => new RecipientsUser { Email = x.Email, EntityId = x.Id, OrganizationEntityId = x.CompanyId, UserType = UserType.Company }).ToList(),
                        Subject = subject,
                        SystemEventType = (int)SystemEventsTypes.BidLimitedOffersChangedEmail
                    }, ex);
                    throw;
                }
            }
            else
            {
                var emailRequest = new EmailRequestMultipleRecipients
                {
                    Body = html,
                    Attachments = null,
                    Recipients = providersEmails.Select(x => new RecipientsUser { Email = x.Email }).ToList(),
                    Subject = subject,
                    SystemEventType = (int)SystemEventsTypes.BidLimitedOffersChangedEmail
                };
                await emailService.SendToMultipleReceiversAsync(emailRequest);
            }
            return (true, string.Empty, string.Empty, providersEmails.Count);
        }

        private async Task UpdateBidStatus(long bidId)
        {
            try
            {
                var bid = (await _bidRepository.FindAsync(b => b.Id == bidId &&
                    !b.IsDeleted
                    && b.BidStatusId != (int)TenderStatus.Draft
                    && b.BidTypeId != (int)BidTypes.Instant, false, nameof(Bid.BidAddressesTime))).FirstOrDefault();

                if (bid.BidAddressesTime != null && bid.BidStatusId != (int)TenderStatus.Draft)
                {
                    // under evaluation(today is greater than OffersOpeningDate and less than ConfirmationDate or confirmation date is null)
                    if (DateTime.Compare(_dateTimeZone.CurrentDate, (DateTime)bid.BidAddressesTime.OffersOpeningDate) >= 0 &&
                            (bid.BidAddressesTime.ConfirmationDate == null || DateTime.Compare(_dateTimeZone.CurrentDate, bid.BidAddressesTime.ConfirmationDate.Value) < 0))
                        if (bid.BidStatusId == (int)TenderStatus.Open)
                            bid.BidStatusId = (int)TenderStatus.Evaluation;
                    // stopping period(today is greater than ConfirmationDate and less than or equal ConfirmationDate + stopping period)
                    if (bid.BidAddressesTime.ConfirmationDate.HasValue &&
                        DateTime.Compare(_dateTimeZone.CurrentDate, bid.BidAddressesTime.ConfirmationDate.Value) >= 0 &&
                        DateTime.Compare(_dateTimeZone.CurrentDate, bid.BidAddressesTime.ConfirmationDate.Value.AddDays(bid.BidAddressesTime.StoppingPeriod)) <= 0)
                        if (bid.BidStatusId == (int)TenderStatus.Evaluation)
                            bid.BidStatusId = (int)TenderStatus.Stopping;
                    // under awarding(today is greater than ConfirmationDate + stopping period and less than or equal to ExpectedAnchoringDate)
                    if (bid.BidAddressesTime.ConfirmationDate.HasValue &&
                        DateTime.Compare(_dateTimeZone.CurrentDate, bid.BidAddressesTime.ConfirmationDate.Value.AddDays(bid.BidAddressesTime.StoppingPeriod)) > 0 &&
                        DateTime.Compare(_dateTimeZone.CurrentDate, bid.BidAddressesTime.ExpectedAnchoringDate.Value) <= 0)
                        if (bid.BidStatusId == (int)TenderStatus.Stopping)
                            bid.BidStatusId = (int)TenderStatus.Awarding;
                }
                await _bidRepository.Update(bid);
            }
            catch (Exception ex)
            {
                _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = bidId,
                    ErrorMessage = "Failed to update Bid status!",
                    ControllerAndAction = $"{nameof(BidService)}/{nameof(UpdateBidStatus)}"
                });
            }
        }
        private async Task MapAllBidsResultForVisitor(List<Bid> result, List<GetMyBidResponse> bidsModels)
        {
            foreach (var itm in bidsModels)
            {
                var bid = result.FirstOrDefault(bid => bid.Id == itm.Id);
                if (bid.EntityType == UserType.Association)
                {
                    itm.EntityName = bid.Association.Association_Name;
                    itm.EntityImage = bid.Association.Image;
                    itm.EntityImageFileName = bid.Association.ImageFileName;
                }
                else if (bid.EntityType == UserType.Donor)
                {

                    itm.EntityName = bid.Donor.DonorName;
                    itm.EntityImage = bid.Donor.Image;
                    itm.EntityImageFileName = bid.Donor.ImageFileName;
                }

                itm.EntityType = bid.AssociationId.HasValue ? UserType.Association : UserType.Donor;
                itm.EntityId = bid.AssociationId.HasValue ? bid.AssociationId.Value : bid.DonorId.Value;
                itm.EntityLogoResponse = await _imageService.GetFileResponseEncrypted(itm.EntityImage);
                itm.EntityImage = null;
            }
        }

        public async Task<OperationResult<GetEntityContactDetailsResponse>> GetEntityContactDetails(GetEntityContactDetailsRequest request)
        {
            var user = _currentUserService.CurrentUser;
            if (user is null)
                return OperationResult<GetEntityContactDetailsResponse>.Fail(HttpErrorCode.NotAuthorized, CommonErrorCodes.NotAuthorized);
            var bid = await _bidRepository.FindOneAsync(x => x.Id == request.BidId);
            if (bid is null)
                return OperationResult<GetEntityContactDetailsResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);

            var isUserHasAccess= await checkIfParticipantCanAccessBidData(request.BidId, user);
            if (!isUserHasAccess.IsSucceeded)
                return OperationResult<GetEntityContactDetailsResponse>.Fail(isUserHasAccess.HttpErrorCode, isUserHasAccess.Code);
            if (request.UserType == UserType.Association)
            {
                var association = await _associationRepository.Find(x => x.Id == request.Id)
                    .Select(x => new GetEntityContactDetailsResponse()
                    {
                        Id = x.Id,
                        Email = x.Email,
                        Manager_Email = x.Manager_Email,
                        Manager_FullName = x.Manager_FullName,
                        Manager_Mobile = x.Manager_Mobile,
                        Mobile = x.Mobile,
                    })
                    .FirstOrDefaultAsync();
                if (association is null)
                    return OperationResult<GetEntityContactDetailsResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.ASSOCIATION_NOT_FOUND);
                return OperationResult<GetEntityContactDetailsResponse>.Success(association);
            }
            if (request.UserType == UserType.Donor)
            {
                var donor = await _donorRepository.Find(x => x.Id == request.Id)
                    .Select(x => new GetEntityContactDetailsResponse()
                    {
                        Id = x.Id,
                        Email = x.DonorEmail,
                        Manager_Email = x.ManagerEmail,
                        Manager_FullName = x.ManagerFullName,
                        Manager_Mobile = x.ManagerMobile,
                        Mobile = x.DonorNumber,
                    })
                    .FirstOrDefaultAsync();
                if (donor is null)
                    return OperationResult<GetEntityContactDetailsResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.DONOR_NOT_FOUND);
                return OperationResult<GetEntityContactDetailsResponse>.Success(donor);
            }
            return OperationResult<GetEntityContactDetailsResponse>.Fail(HttpErrorCode.InvalidInput, CommonErrorCodes.INVALID_INPUT);

        }

        private async Task<OperationResult<bool>> checkIfParticipantCanAccessBidData(long bidId, ApplicationUser user)
        {

            if (user is null)
                return OperationResult<bool>.Fail(HttpErrorCode.NotAuthorized, CommonErrorCodes.NotAuthorized);
            var isFeaturesEnabled = await _appGeneralSettingsRepository
                 .Find(x => true).Select(x => x.IsSubscriptionFeaturesEnabled)
                 .FirstOrDefaultAsync();
            var bid = await _bidRepository.FindOneAsync(x => x.Id == bidId);

            if ((user.UserType == UserType.Provider|| user.UserType == UserType.Freelancer) && isFeaturesEnabled&& bid.IsApplyOfferWithSubscriptionMandatory != true)
            {
                var isBoughtTermsBook = await _providerBidRepository.Find(x => x.IsPaymentConfirmed && x.BidId == bidId)
                        .WhereIf(user.UserType == UserType.Provider, x => x.CompanyId == user.CurrentOrgnizationId)
                        .WhereIf(user.UserType == UserType.Freelancer, x => x.FreelancerId == user.CurrentOrgnizationId)
                        .AnyAsync();
                var isRevealedBid = await _subscriptionPaymentFeatureUsageRepository.Find(x => x.BidId == bidId)
                    .WhereIf(user.UserType == UserType.Provider, x => x.CompanyId == user.CurrentOrgnizationId)
                    .WhereIf(user.UserType == UserType.Freelancer, x => x.FreelancerId == user.CurrentOrgnizationId)
                    .Include(a => a.subscriptionPaymentFeature)
                    .FirstOrDefaultAsync();
                var userType = user.GetUserType();
                var firstFeature = isRevealedBid?.subscriptionPaymentFeature;
                if (firstFeature is null)
                {
                    var subscriptionPayment = await _subscriptionPaymentRepository
                        .Find(x => !x.IsExpired && x.IsPaymentConfirmed && x.UserId == user.CurrentOrgnizationId && x.UserTypeId == userType)
                        .Include(x => x.SubscriptionPaymentFeatures)
                        .Include(x => x.SubscriptionPackagePlan)
                        .AsSplitQuery()
                        .OrderByDescending(x => x.CreationDate)
                        .FirstOrDefaultAsync();
                    // return OperationResult<ReadOnlyBidResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);
                    if (subscriptionPayment is not null)
                        firstFeature = subscriptionPayment.SubscriptionPaymentFeatures.FirstOrDefault();
                }
                var isPremiumPackage =
                     (firstFeature != null && (firstFeature.ValueType == FeatureValueType.Count &&
                     firstFeature.Count.HasValue && firstFeature.Count.Value == int.MaxValue)) || (firstFeature != null && firstFeature.Count is null);

                if (!isBoughtTermsBook && isRevealedBid == null && !isPremiumPackage)
                    return OperationResult<bool>.Fail(HttpErrorCode.BusinessRuleViolation, CommonErrorCodes.NOT_REVEALED_AND_NOT_BOUGHT_TERMSbOOK);
            }
            return OperationResult<bool>.Success(true);
        }

        public async Task<OperationResult<bool>> RevealBidInCaseNotSubscribe(long bidId)
        {

            var user = _currentUserService.CurrentUser;
            if (user is null)
                return OperationResult<bool>.Fail(HttpErrorCode.NotAuthorized, CommonErrorCodes.NotAuthorized);

            var isFeaturesEnabled = await _appGeneralSettingsRepository
                .Find(x => true).Select(x => x.IsSubscriptionFeaturesEnabled)
                .FirstOrDefaultAsync();

            if(!isFeaturesEnabled)
                return OperationResult<bool>.Success(true);


            var userType = user.GetUserType();

            var bid = await _bidRepository.FindOneAsync(x => x.Id == bidId);
            if (bid is null)
                return OperationResult<bool>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.NOT_FOUND);

            var subscriptionPayment = await _subscriptionPaymentRepository
                    .Find(x => !x.IsExpired && x.IsPaymentConfirmed && x.UserId == user.CurrentOrgnizationId && x.UserTypeId == userType)
                    .Include(x => x.SubscriptionPaymentFeatures.Where(a=>a.ValueType== FeatureValueType.Count).Take(1))
                    .Include(x => x.SubscriptionPackagePlan)
                    .AsSplitQuery()
                    .OrderByDescending(x => x.CreationDate)
                    .FirstOrDefaultAsync();
            var firstFeature = subscriptionPayment?.SubscriptionPaymentFeatures?.FirstOrDefault();
            var remaining = firstFeature is not null && firstFeature.UsageCount.HasValue ?
                ((firstFeature.Count ?? 0) - (firstFeature.UsageCount ?? 0)) : 0;

            await _bidRevealLogRepository.Add(new BidRevealLog
            {
                BidId = bid.Id,
                SubscriptionPaymentId = subscriptionPayment?.Id,
                Status = subscriptionPayment is null?
                        BidRevealStatus.NotHasSubscription:
                        (firstFeature is not null?
                        (remaining<=0? BidRevealStatus.HasNoCredit: BidRevealStatus.TryToBuyTermsBook)
                        : BidRevealStatus.HasNoCredit),
                FreelancerId= userType == UserType.Freelancer? user.CurrentOrgnizationId:null,
                CompanyId= userType == UserType.Company ? user.CurrentOrgnizationId : null
            });

            return OperationResult<bool>.Success(true);

        }

        public async Task<OperationResult<BidViewsStatisticsResponse>> GetBidViewsStatisticsAsync(long bidId)
        {
            try
            {
                if(_currentUserService.IsUserNotAuthorized(new List<UserType> { UserType.SuperAdmin, UserType.Admin}))
                    return OperationResult<BidViewsStatisticsResponse>.Fail(HttpErrorCode.NotAuthorized, CommonErrorCodes.NotAuthorized);

                var bid = await _bidRepository.FindOneAsync(x => x.Id == bidId && x.BidStatusId != (int)TenderStatus.Draft && x.BidStatusId != (int)TenderStatus.Pending && x.BidStatusId != (int)TenderStatus.Reviewing);
                if(bid is null)
                    return OperationResult<BidViewsStatisticsResponse>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);

                var normalBidViewLogs = await _bidViewsLogRepository.Find(x => x.BidId == bid.Id)
                    .Include(x => x.Organization)
                    .ToListAsync();
                var actionedBidViewLogs = await _bidRevealLogRepository.Find(x => x.BidId == bid.Id)
                    .Select(x => new { Status = x.Status, EntityId = x.CompanyId != null ? x.CompanyId : x.FreelancerId, EntityType = x.CompanyId != null ? UserType.Company : UserType.Freelancer})
                    .ToListAsync();


                var registeredEntityWithoutActionViewsCount = normalBidViewLogs.Where(x => x.OrganizationId.HasValue 
                /*&& !actionedBidViewLogs.Any(a => x.Organization.EntityID == a.EntityId && _helperService.GetUserTypeFromOrganizationType(x.Organization.OrgTypeID) == a.EntityType)*/)
                    .Count();

                var anonymousViews = new GetBidViewGroupStatistics
                {
                    BidViewGroup = BidViewGroups.Anonymous,
                    RevealViewStatus = null,
                    ViewsCount = normalBidViewLogs.Count(x => x.OrganizationId == null),
                };
                var registeredEntityWithoutActionViews = new GetBidViewGroupStatistics
                {
                    BidViewGroup = BidViewGroups.RegisteredEntityWithoutAction,
                    RevealViewStatus = null,
                    ViewsCount = registeredEntityWithoutActionViewsCount,
                };

                var res = new BidViewsStatisticsResponse
                {
                    TotalViewsCount = normalBidViewLogs.Count,
                    BidViewGroupsStatistics = new List<GetBidViewGroupStatistics> { anonymousViews, registeredEntityWithoutActionViews },
                };

                var statusesGroups = actionedBidViewLogs.GroupBy(x => x.Status);
                foreach (BidRevealStatus status in Enum.GetValues(typeof(BidRevealStatus)))
                {
                    var groupWithSameStatus = statusesGroups.Where(x => x.Key == status).FirstOrDefault();
                    var obj = new GetBidViewGroupStatistics
                    {
                        BidViewGroup = BidViewGroups.RegisteredEntityWithAction,
                        RevealViewStatus = status,
                        ViewsCount = groupWithSameStatus is null ? 0 : groupWithSameStatus.Count(),
                    };
                    res.BidViewGroupsStatistics.Add(obj);
                }
                return OperationResult<BidViewsStatisticsResponse>.Success(res);
            }
            catch (Exception ex)
            {
                string refNo = _logger.Log(new LoggerModel
                {
                    ExceptionError = ex,
                    UserRequestModel = $"bidid = {bidId}",
                    ErrorMessage = "Failed to Get Bid Views Statistics Async!",
                    ControllerAndAction = "Bid/BidViewsStatistics"
                });
                return OperationResult<BidViewsStatisticsResponse>.Fail(HttpErrorCode.ServerError, CommonErrorCodes.OPERATION_FAILED, refNo);
            }
        }
    }
}