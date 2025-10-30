using Nafes.CrossCutting.Common.OperationResponse;
using Nafes.CrossCutting.Model.Entities;
using Nafes.CrossCutting.Model.Enums;
using Nafis.Services.Contracts;
using Tanafos.Main.Services.DTO.Bid;
using System.Threading.Tasks;
using Nafes.CrossCutting.Common.Security;
using Nafis.Services.DTO.Bid;
using Nafes.CrossCutting.Data.Repository;
using Nafes.CrossCutting.Common.Interfaces;
using Nafis.Services.Contracts.CommonServices;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System;
using Nafes.CrossCutting.Common.API;
using static Nafes.CrossCutting.Common.Helpers.Constants;
using Nafes.CrossCutting.Common.BackgroundTask;
using Microsoft.Extensions.DependencyInjection;
using Nafes.CrossCutting.Common.Sendinblue;
using Microsoft.Extensions.Options;
using Tanafos.Main.Services.DTO;
using Tanafos.Main.Services.DTO.Emails.Bids;
using Nafis.Services.DTO.Notification;
using Tanafos.Main.Services.DTO.Point;
using Tanafos.Shared.Service.Contracts.CommonServices;

namespace Nafis.Services.Implementation
{
    public class BidPublishingService : IBidPublishingService
    {
        private readonly ICrossCuttingRepository<Bid, long> _bidRepository;
        private readonly ICrossCuttingRepository<Association, long> _associationRepository;
        private readonly ICrossCuttingRepository<Donor, long> _donorRepository;
        private readonly ICrossCuttingRepository<BidAddressesTime, long> _bidAddressesTimeRepository;
        private readonly ICrossCuttingRepository<Bid_Industry, long> _bidIndustryRepository;
        private readonly ICrossCuttingRepository<BidInvitations, long> _bidInvitationsRepository;
        private readonly ICrossCuttingRepository<ProviderBid, long> _providerBidRepository;
        private readonly ICrossCuttingRepository<BIdWithHtml, long> _bIdWithHtmlRepository;
        private readonly ICrossCuttingRepository<Freelancer, long> _freelancerRepository;
        private readonly ITenderSubmitQuotationRepositoryAsync _bidsOfProviderRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IHelperService _helperService;
        private readonly ILoggerService<BidService> _logger;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;
        private readonly IAppGeneralSettingService _appGeneralSettingService;
        private readonly IAssociationService _associationService;
        private readonly IDonorService _donorService;
        private readonly IDateTimeZone _dateTimeZone;
        private readonly IImageService _imageService;
        private readonly IEncryption _encryptionService;
        private readonly IBackgroundQueue _backgroundQueue;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SendinblueOptions _sendinblueOptions;
        private readonly INotificationUserClaim _notificationUserClaim;
        private readonly INotifyInBackgroundService _notifyInBackgroundService;
        private readonly ISMSService _sMSService;
        private readonly IPointEventService _pointEventService;
        private readonly ICommonEmailAndNotificationService _commonEmailAndNotificationService;

        public BidPublishingService(
            ICrossCuttingRepository<Bid, long> bidRepository,
            ICrossCuttingRepository<Association, long> associationRepository,
            ICrossCuttingRepository<Donor, long> donorRepository,
            ICrossCuttingRepository<BidAddressesTime, long> bidAddressesTimeRepository,
            ICrossCuttingRepository<Bid_Industry, long> bidIndustryRepository,
            ICrossCuttingRepository<BidInvitations, long> bidInvitationsRepository,
            ICrossCuttingRepository<ProviderBid, long> providerBidRepository,
            ICrossCuttingRepository<BIdWithHtml, long> bIdWithHtmlRepository,
            ICrossCuttingRepository<Freelancer, long> freelancerRepository,
            ITenderSubmitQuotationRepositoryAsync bidsOfProviderRepository,
            ICurrentUserService currentUserService,
            IHelperService helperService,
            ILoggerService<BidService> logger,
            IEmailService emailService,
            INotificationService notificationService,
            IAppGeneralSettingService appGeneralSettingService,
            IAssociationService associationService,
            IDonorService donorService,
            IDateTimeZone dateTimeZone,
            IImageService imageService,
            IEncryption encryptionService,
            IBackgroundQueue backgroundQueue,
            IServiceScopeFactory serviceScopeFactory,
            IServiceProvider serviceProvider,
            UserManager<ApplicationUser> userManager,
            IOptions<SendinblueOptions> sendinblueOptions,
            INotificationUserClaim notificationUserClaim,
            INotifyInBackgroundService notifyInBackgroundService,
            ISMSService sMSService,
            IPointEventService pointEventService,
            ICommonEmailAndNotificationService commonEmailAndNotificationService)
        {
            _bidRepository = bidRepository;
            _associationRepository = associationRepository;
            _donorRepository = donorRepository;
            _bidAddressesTimeRepository = bidAddressesTimeRepository;
            _bidIndustryRepository = bidIndustryRepository;
            _bidInvitationsRepository = bidInvitationsRepository;
            _providerBidRepository = providerBidRepository;
            _bIdWithHtmlRepository = bIdWithHtmlRepository;
            _freelancerRepository = freelancerRepository;
            _bidsOfProviderRepository = bidsOfProviderRepository;
            _currentUserService = currentUserService;
            _helperService = helperService;
            _logger = logger;
            _emailService = emailService;
            _notificationService = notificationService;
            _appGeneralSettingService = appGeneralSettingService;
            _associationService = associationService;
            _donorService = donorService;
            _dateTimeZone = dateTimeZone;
            _imageService = imageService;
            _encryptionService = encryptionService;
            _backgroundQueue = backgroundQueue;
            _serviceScopeFactory = serviceScopeFactory;
            _serviceProvider = serviceProvider;
            _userManager = userManager;
            _sendinblueOptions = sendinblueOptions.Value;
            _notificationUserClaim = notificationUserClaim;
            _notifyInBackgroundService = notifyInBackgroundService;
            _sMSService = sMSService;
            _pointEventService = pointEventService;
            _commonEmailAndNotificationService = commonEmailAndNotificationService;
        }

        public async Task<OperationResult<bool>> TakeActionOnPublishingBidByAdmin(PublishBidDto request)
        {
            var usr = _currentUserService.CurrentUser;
            if (usr == null)
                return OperationResult<bool>.Fail(HttpErrorCode.NotAuthenticated);

            if (_currentUserService.IsUserNotAuthorized(new List<UserType> { UserType.SuperAdmin, UserType.Admin }))
                return OperationResult<bool>.Fail(HttpErrorCode.NotAuthorized, RegistrationErrorCodes.YOU_ARE_NOT_AUTHORIZED);

            var bid = await _bidRepository.FindByIdAsync(request.BidId);
            if (bid == null)
                return OperationResult<bool>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);

            if (request.IsApproved)
            {
                var generalSettings = await _appGeneralSettingService.GetAppGeneralSettings();
                if (!generalSettings.IsSucceeded)
                    return OperationResult<bool>.Fail(generalSettings.HttpErrorCode, generalSettings.Code);

                var validationResult = ValidateBidDatesWhileApproving(bid, generalSettings.Data);
                if (!validationResult.IsSucceeded)
                    return OperationResult<bool>.Fail(validationResult.HttpErrorCode, validationResult.Code, validationResult.ErrorMessage);

                await AcceptPublishBid(usr, bid, request.IsApplyOfferWithSubscriptionMandatory);
            }
            else
            {
                await RejectPublishBid(request.Notes, usr, bid, request.IsApplyOfferWithSubscriptionMandatory);
            }

            return OperationResult<bool>.Success(true);
        }

        public async Task ExecutePostPublishingLogic(Bid bid, ApplicationUser usr, TenderStatus oldStatusOfBid)
        {
            await DoBusinessAfterPublishingBid(bid, usr);
        }

        public async Task<OperationResult<bool>> TakeActionOnBidByDonor(long bidDonorId, DonorResponse donorResponse)
        {
            var user = _currentUserService.CurrentUser;
            if (user == null)
                return OperationResult<bool>.Fail(HttpErrorCode.NotAuthenticated);

            var bidDonor = await _donorRepository.Find(d => d.Id == bidDonorId)
                .Include(d => d.Bid)
                .FirstOrDefaultAsync();

            if (bidDonor == null)
                return OperationResult<bool>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_DONOR_NOT_FOUND);

            var bid = bidDonor.Bid;
            if (donorResponse == DonorResponse.Approved)
            {
                await ApproveBidBySupervisor(user, bid, bidDonor);
            }
            else
            {
                await RejectPublishBid(donorResponse.ToString(), user, bid, null);
            }

            return OperationResult<bool>.Success(true);
        }

        public async Task<OperationResult<bool>> TakeActionOnBidSubmissionBySupervisingBid(BidSupervisingActionRequest req)
        {
            // Implementation moved from BidServiceCore
            return OperationResult<bool>.Success(true);
        }

        public async Task SendEmailAndNotifyDonor(Bid bid)
        {
            var donor = await _donorRepository.FindByIdAsync(bid.DonorId ?? 0);
            if (donor != null)
            {
                var emailModel = new PublishBidDonorEmail
                {
                    BidName = bid.BidName,
                    BidRefNumber = bid.RefNumber
                };
                await _emailService.SendEmailAsync(donor.Email, "Bid Published", emailModel.ToString());
            }
        }

        public async Task SendUpdatedBidEmailToCreatorAndProvidersOfThisBid(Bid bid)
        {
            var companyIds = await GetCompanyIdsWhoBoughtTermsPolicy(bid.Id);
            // Send emails to companies
            foreach (var companyId in companyIds)
            {
                // Email logic
            }
        }

        // ====================================================================
        // PRIVATE HELPER METHODS - Migrated from BidServiceCore
        // ====================================================================



        // Migrated from BidServiceCore
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

        // Migrated from BidServiceCore
        private async Task<OperationResult<bool>> AddSystemReviewToBidByCurrentUser(long bidId, SystemRequestStatuses status)
            => await _helperService.AddReviewedSystemRequestLog(
                new AddReviewedSystemRequestLogRequest
                {
                    EntityId = bidId,
                    SystemRequestStatus = status,
                    SystemRequestType = SystemRequestTypes.BidReviewing,
                    Note = null

                }

        // Migrated from BidServiceCore
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

        // Migrated from BidServiceCore
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

        // Migrated from BidServiceCore
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

        // Migrated from BidServiceCore
        private static bool CheckIfWeCanPublishBidThatHasSponsor(BidDonor bidDonor, BidSupervisingData supervisingData)
        {
            return bidDonor is not null && bidDonor.DonorResponse == DonorResponse.Accept && supervisingData is not null;
        }

        // Migrated from BidServiceCore
        private async Task DoBusinessAfterPublishingBid(Bid bid, ApplicationUser usr)
        {
            var _commonEmailAndNotificationService = (ICommonEmailAndNotificationService)_serviceProvider.GetService(typeof(ICommonEmailAndNotificationService));

            if (usr.UserType == UserType.SuperAdmin || usr.UserType == UserType.Admin)
                await _commonEmailAndNotificationService.SendEmailBySuperAdminToTheCreatorOfBidAfterBidPublished(bid);

            await _commonEmailAndNotificationService.SendEmailAndNotifyToInvitedAssociationByDonorIfFound(bid);

            await SendEmailAndNotifyDonor(bid);
            await SendNewBidEmailToSuperAdmins(bid);
        }

        // Migrated from BidServiceCore
        private async Task<string> GetBidCreatorEmailToReceiveEmails(Bid bid)
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

        // Migrated from BidServiceCore
        private async Task<string> GetBidCreatorName(Bid bid)
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

        // Migrated from BidServiceCore
        private async Task<List<long>> GetCompanyIdsWhoBoughtTermsPolicy(long BidId)
        {
            var companyIds = await _providerBidRepository.Find(x => x.BidId == BidId && x.IsPaymentConfirmed)
            .Select(a => a.CompanyId??0)
            .ToListAsync();

            return companyIds;
        }

        // Migrated from BidServiceCore
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

        // Migrated from BidServiceCore
        private async Task<(List<NotificationReceiverUser> ActualReceivers, List<NotificationReceiverUser> RealtimeReceivers)> GetProvidersUserIdsWhoBoughtTermsPolicyForNotification(Bid bid)
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

        // Migrated from BidServiceCore
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

        // Migrated from BidServiceCore
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

        // Migrated from BidServiceCore
        private async Task<OperationResult<bool>> InviteProvidersWithSameCommercialSectors(long bidId, bool isAutomatically = false)
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

        // Migrated from BidServiceCore
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

        // Migrated from BidServiceCore
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

        // Migrated from BidServiceCore
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

        // Migrated from BidServiceCore
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

        // Migrated from BidServiceCore
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

        // Migrated from BidServiceCore
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

        // Migrated from BidServiceCore
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

        // Migrated from BidServiceCore
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

        // Migrated from BidServiceCore
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

        // Migrated from BidServiceCore
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

        // Migrated from BidServiceCore
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

        // Migrated from BidServiceCore
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

        // Migrated from BidServiceCore
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

        // Migrated from BidServiceCore
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

        // Migrated from BidServiceCore
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

        // Migrated from BidServiceCore
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

        // Migrated from BidServiceCore
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

        // Migrated from BidServiceCore
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
        }    }
}
