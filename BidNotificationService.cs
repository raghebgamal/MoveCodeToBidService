using Nafes.CrossCutting.Common.OperationResponse;
using Nafes.CrossCutting.Model.Entities;
using Nafis.Services.Contracts;
using Tanafos.Main.Services.DTO.Bid;
using Tanafos.Main.Services.DTO.ReviewedSystemRequestLog;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nafis.Services.DTO.Company;
using Nafes.CrossCutting.Common.ReviewedSystemRequestLog;
using Nafis.Services.DTO.Notification;
using Nafes.CrossCutting.Data.Repository;
using Nafes.CrossCutting.Common.Interfaces;
using Nafis.Services.Contracts.CommonServices;
using Nafes.CrossCutting.Common.BackgroundTask;
using Microsoft.Extensions.DependencyInjection;
using Nafes.CrossCutting.Common.Sendinblue;
using Microsoft.Extensions.Options;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System;
using Nafes.CrossCutting.Common.API;
using Nafes.CrossCutting.Model.Enums;
using static Nafes.CrossCutting.Common.Helpers.Constants;
using Nafes.CrossCutting.Common.Settings;

namespace Nafis.Services.Implementation
{
    public class BidNotificationService : IBidNotificationService
    {
        private readonly ICrossCuttingRepository<BidInvitations, long> _bidInvitationsRepository;
        private readonly ICrossCuttingRepository<Bid, long> _bidRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IHelperService _helperService;
        private readonly ILoggerService<BidService> _logger;
        private readonly INotificationService _notificationService;
        private readonly ICrossCuttingRepository<ProviderBid, long> _providerBidRepository;
        private readonly IReviewedSystemRequestLogService _reviewedSystemRequestLogService;
        private readonly IBackgroundQueue _backgroundQueue;
        private readonly INotificationUserClaim _notificationUserClaim;
        private readonly SendinblueOptions _sendinblueOptions;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly FileSettings fileSettings;

        public BidNotificationService(
            ICrossCuttingRepository<BidInvitations, long> bidInvitationsRepository,
            ICrossCuttingRepository<Bid, long> bidRepository,
            ICurrentUserService currentUserService,
            IHelperService helperService,
            ILoggerService<BidService> logger,
            INotificationService notificationService,
            ICrossCuttingRepository<ProviderBid, long> providerBidRepository,
            IReviewedSystemRequestLogService reviewedSystemRequestLogService,
            IBackgroundQueue backgroundQueue,
            INotificationUserClaim notificationUserClaim,
            IOptions<SendinblueOptions> sendinblueOptions,
            IServiceScopeFactory serviceScopeFactory,
            IOptions<FileSettings> fileSettings)
        {
            _bidInvitationsRepository = bidInvitationsRepository;
            _bidRepository = bidRepository;
            _currentUserService = currentUserService;
            _helperService = helperService;
            _logger = logger;
            _notificationService = notificationService;
            _providerBidRepository = providerBidRepository;
            _reviewedSystemRequestLogService = reviewedSystemRequestLogService;
            _backgroundQueue = backgroundQueue;
            _notificationUserClaim = notificationUserClaim;
            _sendinblueOptions = sendinblueOptions.Value;
            _serviceScopeFactory = serviceScopeFactory;
            this.fileSettings = fileSettings.Value;
        }

        public async Task<OperationResult<bool>> InviteProvidersWithSameCommercialSectors(long bidId, bool isAutomatically = false)
        {
            var user = _currentUserService.CurrentUser;
            if (user == null)
                return OperationResult<bool>.Fail(HttpErrorCode.NotAuthenticated);

            var bid = await _bidRepository.FindByIdAsync(bidId);
            if (bid == null)
                return OperationResult<bool>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);

            _backgroundQueue.QueueTask(async (scopeServiceProvider, cancellationToken) =>
            {
                await InviteProvidersInBackground(bid, isAutomatically, user);
            });

            return OperationResult<bool>.Success(true);
        }

        public async Task<OperationResult<List<InvitedCompanyResponseDto>>> GetAllInvitedCompaniesForBidAsync(GetAllInvitedCompaniesRequestModel request)
        {
            try
            {
                var result = await GetAllInvitedCompaniesResponseForBid(request);
                return OperationResult<List<InvitedCompanyResponseDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.Log(ex);
                return OperationResult<List<InvitedCompanyResponseDto>>.Fail(HttpErrorCode.InternalServerError, CommonErrorCodes.INTERNAL_SERVER_ERROR);
            }
        }

        public async Task<OperationResult<List<GetReviewedSystemRequestLogResponse>>> GetProviderInvitationLogs(long bidId)
        {
            try
            {
                var invitationLogs = await _reviewedSystemRequestLogService.GetMultibleReviewedSystemRequestLogsAsync(new List<long> { bidId }, ReviewedSystemEnum.BidInvitation);

                if (!invitationLogs.IsSucceeded)
                    return OperationResult<List<GetReviewedSystemRequestLogResponse>>.Fail(invitationLogs.HttpErrorCode, invitationLogs.Code);

                return OperationResult<List<GetReviewedSystemRequestLogResponse>>.Success(invitationLogs.Data);
            }
            catch (Exception ex)
            {
                _logger.Log(ex);
                return OperationResult<List<GetReviewedSystemRequestLogResponse>>.Fail(HttpErrorCode.InternalServerError, CommonErrorCodes.INTERNAL_SERVER_ERROR);
            }
        }

        public async Task<(List<NotificationReceiverUser> ActualReceivers, List<NotificationReceiverUser> RealtimeReceivers)> GetProvidersUserIdsWhoBoughtTermsPolicyForNotification(Bid bid)
        {
            var companyIds = await GetCompanyIdsWhoBoughtTermsPolicy(bid.Id);
            var usersIds = await _notificationUserClaim.GetUsersClaimOfMultipleIds(companyIds, UserType.Company);
            return usersIds;
        }


        // ====================================================================
        // PRIVATE HELPER METHODS - Migrated from BidServiceCore
        // ====================================================================



        // Migrated from BidServiceCore
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

        // Migrated from BidServiceCore
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
        }    }
}
