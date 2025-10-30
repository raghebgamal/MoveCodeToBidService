# Complete BidServiceCore Migration Plan

## Objective
Move ALL method implementations from BidServiceCore to individual services, so BidServiceCore can be completely removed.

## Current State
- **BidServiceCore**: 139 public methods, 10,427 lines
- **7 Services**: Each calls specific methods from BidServiceCore via `_bidServiceCore.MethodName()`

## Migration Scope by Service

### 1. BidNotificationService (SMALLEST - 4 methods)
**Methods to migrate:**
1. `InviteProvidersWithSameCommercialSectors(long bidId, bool isAutomatically)`
2. `GetAllInvitedCompaniesForBidAsync(GetAllInvitedCompaniesRequestModel request)`
3. `GetProviderInvitationLogs(long bidId)`
4. `GetProvidersUserIdsWhoBoughtTermsPolicyForNotification(Bid bid)`

**Additional helper methods these call:**
- `GetAllInvitedCompaniesResponseForBid`
- `GetAllInvitedCompaniesModels`
- `SendEmailToCompaniesInBidIndustry`
- `GetFreelancersWithSameWorkingSectors`
- `SendSMSForProvidersWithSameCommercialSectors`
- `SendNotificationsOfBidAdded`
- `InviteProvidersInBackground`
- `GetCompanyIdsWhoBoughtTermsPolicy`

**Total: ~12 methods**

---

### 2. BidPublishingService (6 public + helpers)
**Methods to migrate:**
1. `TakeActionOnPublishingBidByAdmin(PublishBidDto request)`
2. `ExecutePostPublishingLogic(Bid bid, ApplicationUser usr, TenderStatus oldStatusOfBid)`
3. `TakeActionOnBidByDonor(long bidDonorId, DonorResponse donorResponse)`
4. `TakeActionOnBidSubmissionBySupervisingBid(BidSupervisingActionRequest req)`
5. `SendEmailAndNotifyDonor(Bid bid)`
6. `SendUpdatedBidEmailToCreatorAndProvidersOfThisBid(Bid bid)`

**Additional helper methods:**
- `RejectPublishBid`
- `SendAdminRejectedBidEmail`
- `SendAdminRejectBidNotification`
- `AcceptPublishBid`
- `ValidateBidDatesWhileApproving`
- `ApplyDefaultFlowOfApproveBid`
- `ApplyPrivateBidLogicWithNoSponsor`
- `CheckIfWeCanPublishBidThatHasSponsor`
- `SaveRFPAsPdf`
- `ApproveBidBySupervisor`
- `DoBusinessAfterPublishingBid`
- `SendEmailToAssociationWhenDonorApproveBidSubmission`
- `SendNotificationToAssociationWhenDonorApproveBidSubmission`
- `SendEmailToAssociationWhenDonorRejectBidSubmission`
- `SendNotificationToAssociationWhenDonorRejectBidSubmission`
- `SendSMSPublishBidToProvider`

**Total: ~22 methods**

---

### 3. BidPaymentService (7 public + helpers)
**Methods to migrate:**
1. `GetBidPrice(GetBidDocumentsPriceRequestModel request)`
2. `GetBidPriceForFreelancer(GetBidDocumentsPriceRequestModel request)`
3. `BuyTermsBook(BuyTermsBookModel model)`
4. `GetBuyTenderDocsPillModel(long providerBidId)`
5. `GetProviderDataOfRefundableCompanyBid(long companyBidId)`
6. `GetCurrentUserCompaniesToBuyTermsBookWithForbiddenReasonsIfFoundAsync(...)`
7. `GetCurrentUserFreelancersToBuyTermsBookWithForbiddenReasonsIfFoundAsync(...)`

**Additional helper methods:**
- `VerifyCommercialRecordIfAutomatedRegistration`
- `GenerateTransactioNumber`
- `CanCompanyBuyTermsBook`
- `CanFreelancerBuyTermsBook`
- `GetConvinentErrorForBuyTermsBokkForbiddenReasons` (company)
- `GetConvinentErrorForBuyTermsBookForbiddenReasons` (freelancer)
- `ApplyCouponIfExist`
- `LogBuyTenderTermsBookEvent`
- `MapProviderBidTaxes`
- `GetCompanyIdsWhoBoughtTermsPolicy`
- `CreatePremiumPackageUsageTracking`

**Total: ~18 methods**

---

### 4. BidStatisticsService (6 public + helpers)
**Methods to migrate:**
1. `IncreaseBidViewCount(long bidId)` - ✅ **ALREADY MIGRATED**
2. `GetBidViews(long bidId, int pageSize, int pageNumber)`
3. `GetBidViewsStatisticsAsync(long bidId)`
4. `GetBidCreatorId(Bid bid)`
5. `GetProviderBidsWithAssociationFees(...)`
6. `GetTanafosAssociationFeesOfBoughtTermsBooks(IEnumerable<ProviderBid> pbs)`
7. `GetTanafosAssociationFeesOfBoughtTermsBook(ProviderBid pb)`

**Additional helper methods:**
- `IncreaseBidViewCountNew` - ✅ **ALREADY MIGRATED**
- `AddbidViewLog` - ✅ **ALREADY MIGRATED**

**Total: ~7 methods (2 already done)**

---

### 5. BidSearchService (10 public + helpers)
**Methods to migrate:**
1. `GetBidsList(FilterBidsSearchModel request)`
2. `GetBidsCreatedByUser(GetBidsCreatedByUserModel request)`
3. `GetAssociationBids(GetBidsCreatedByUserModel request)`
4. `GetPublicBidsList(int pageSize, int pageNumber)`
5. `GetPublicFreelancingBidsList(FilterBidsSearchModel request)`
6. `GetMyBidsAsync(FilterBidsSearchModel model)`
7. `GetpProviderBids(int pageSize, int pageNumber)`
8. `GetAllBids(FilterBidsSearchModel request)`
9. `GetBidsSearchHeadersAsync()`
10. `GetAssociationProviderBids(int pageSize, int pageNumber)`

**Additional helper methods:**
- `HandlePublicBidsQuery`
- `MapCurrentUserData`
- `MapBidsModels`
- `GetBidsList` (helper)
- `MapEntityData`
- `GetBidsForCurrentUser`
- `ApplyFiltrationForProviderBids`
- `ApplyFiltrationForBids`
- `GetModelItemForBid`
- `IsBidFavoriteByCurrentUser`
- `GetAllBidsForVisitor`
- `GetAllBidsForBidParticipants`
- `GetAssignedForAssociationsOnlyBidsAndThisCompanyBoughtTermsBook`
- `GetAssignedForAssociationsOnlyProviderBidsAndThisCompanyBoughtTermsBook`
- `GetRelatedBidsToBidParticipantWorkingSectorsFirstOrder`
- `MapAllBidsResult`
- `MapBidModelsBasicData`
- `GetPrivateBidsForCurrentAssociation`
- `GetPrivateBidsForCurrentDonor`
- `GetPrivateBidsForSupervisorDonor`
- `MapAllBidsResultForVisitor`

**Total: ~31 methods**

---

### 6. BidManagementService (28 public + helpers)
**Methods to migrate:**
1. `GetBidDetails(long id)`
2. `GetBidMainData(long id)`
3. `GetDetailsForBidByIdAsync(long bidId)`
4. `GetPublicBidDetails(long id)`
5. `GetBidDetailsForShare(long bidId)`
6. `GetBidAddressesTime(long bidId)`
7. `GetBidAttachment(long bidId)`
8. `GetBidNews(long bidId)`
9. `GetBidQuantitiesTable(long bidId)`
10. `GetBidQuantitiesTableNew(long bidId, int pageSize, int pageNumber)`
11. `GetZipFileForBidAttachmentAsBinary(long bidId)`
12. `GetBidStatusDetails(long bidId)`
13. `GetBidStatusWithDates(long bidId)`
14. `GetBidStatusWithDatesForBid(Bid bidInDb)`
15. `GetBidStatusWithDatesForInstantBids(Bid bidInDb)`
16. `OrderTimelineByIndexIfIgnoreTimelineIsTrue(...)`
17. `UpdateReadProviderRead(long id)`
18. `IsBidInEvaluation(long bidId)`
19. `ToggelsAbleToSubscribeToBid(long bidId)`
20. `RevealBidInCaseNotSubscribe(long bidId)`
21. `AddRFIandRequests(AddRFIRequestModel model)`
22. `GetBidRFiRequests(long bidId, int typeId)`
23. `GetUserRole()`
24. `GetStoppingPeriod()`
25. `QuantityStableSettings()`
26. `GetEntityContactDetails(GetEntityContactDetailsRequest request)`
27. `GetBidCreatorName(Bid bid)`
28. `GetBidCreatorEmailToReceiveEmails(Bid bid)`
29. `GetBidCreatorImage(Bid bid)`

**Additional helper methods:** (many mapping methods)
- `GetBidAttachmentNew`
- `MapBasicDataForBidMainData`
- `MapPublicMainData`
- `CheckIfBidIsEditable`
- `FillSupervisingInfo`
- `FillBidDonorInfo`
- `GetBidStatusWithDatesNew`
- `CreateAndFillTheResponseModelForStatuses`
- `SetIsDoneToTrueForPreviousPhases`
- `MoveItemToEnd`
- `GetBidWithRelatedEntitiesByIdAsync`
- `MapRevealsData`
- `CheckIfWeShouldNotShowProviderData`
- `CheckIfBidForAssignedComapniesOnly`
- `MapBidCreatorDetailsIfAssociation`
- `MapBidCreatorDetailsObjectIfDonor`
- `MapBidReview`
- `CheckIfUserCanViewLog`
- `MapPublicData`
- `MapAwardinData`
- `CheckIfCurrentUserIsCreator`
- `MapCancelRequestStatus`
- `FillEvaluationData`
- `ReturnDistinctSupervisingDataBasedOnClaimType`
- `LogToggelsAbleToSubscribeToBidAction`
- `checkIfParticipantCanAccessBidData`

**Total: ~55 methods**

---

### 7. BidCreationService (LARGEST - 56 public + helpers)
**Methods to migrate:** (all the Add/Create/Update methods)
1. `AddBidNew(AddBidModelNew model)`
2. `AddInstantBid(AddInstantBid addInstantBidRequest)`
3. `AddBidAddressesTimes(AddBidAddressesTimesModel model)`
4. `AddBidQuantitiesTable(AddQuantitiesTableRequest model)`
5. `AddBidAttachments(AddBidAttachmentRequest model)`
6. `AddInstantBidAttachments(AddInstantBidsAttachments model)`
7. `UploadBidAttachments(IFormCollection formCollection)`
8. `UploadBidAttachmentsNewsFile(IFormCollection formCollection)`
9. `AddBidClassificationAreaAndExecution(AddBidClassificationAreaAndExecutionModel model)`
10. `AddBidNews(AddBidNewsModel model)`
11. `TenderExtend(AddBidAddressesTimesTenderExtendModel model)`
12. `CopyBid(CopyBidRequest model)`
13. `DeleteDraftBid(long bidId)`

Plus **40+ helper methods** for validation, saving, mapping, etc.

**Total: ~65+ methods**

---

## Migration Strategy

### Phase 1: Small Services First (Build Confidence)
1. **BidNotificationService** (12 methods) - 2-3 hours
2. **BidPublishingService** (22 methods) - 3-4 hours
3. **BidPaymentService** (18 methods) - 3-4 hours
4. **BidStatisticsService** (5 remaining methods) - 1-2 hours

### Phase 2: Medium Services
5. **BidSearchService** (31 methods) - 5-6 hours

### Phase 3: Large Services
6. **BidManagementService** (55 methods) - 8-10 hours
7. **BidCreationService** (65+ methods) - 10-12 hours

### Phase 4: Cleanup
8. Verify BidServiceCore is empty
9. Remove BidServiceCore from DI
10. Delete BidServiceCore.cs
11. Full regression testing

**Total Estimated Time: 35-45 hours of careful work**

## Migration Process for Each Service

### Step 1: Backup
```bash
cp ServiceName.cs ServiceName.cs.backup
cp BidServiceCore.cs BidServiceCore.cs.backup
```

### Step 2: Extract Methods
- Identify all `_bidServiceCore.MethodName()` calls
- Extract those methods + all methods they call (recursive)
- Keep exact implementations - NO CHANGES to logic

### Step 3: Add to Service
- Add all extracted methods as private methods
- Ensure all dependencies are injected in constructor

### Step 4: Update Calls
- Change `_bidServiceCore.MethodName()` to `MethodName()`
- Change `await _bidServiceCore.MethodName()` to `await MethodName()`

### Step 5: Remove from Core
- Delete migrated methods from BidServiceCore
- Verify no other service calls them

### Step 6: Test
- Build solution
- Run all tests
- Manual testing of service functionality

### Step 7: Commit
```bash
git add ServiceName.cs BidServiceCore.cs
git commit -m "Migrate: Move ServiceName methods from BidServiceCore"
git push
```

## Dependencies to Distribute

Each service will need subsets of these 112 dependencies:
- 61 Repository interfaces
- 35 Service interfaces
- 8 External library references
- 3 Settings/Configuration objects
- 5 Other dependencies (UserManager, IMapper, ILogger, IHubContext, IServiceProvider)

## Risks & Mitigation

### Risk 1: Breaking Cross-Service Dependencies
**Mitigation**: Make all methods public initially, then change to private after all services migrated

### Risk 2: Missing Helper Methods
**Mitigation**: Extract recursively - if method A calls method B, migrate both

### Risk 3: Shared Utility Methods
**Mitigation**: Create `BidServiceHelper` class for truly shared utilities, or duplicate where needed

### Risk 4: Database Transaction Boundaries
**Mitigation**: Keep transaction logic identical, test thoroughly

### Risk 5: Testing Coverage
**Mitigation**: Run full test suite after each service migration

## Success Criteria

✅ All services compile without errors
✅ All services have zero references to `_bidServiceCore`
✅ BidServiceCore.cs is empty (only constructor remains)
✅ All existing tests pass
✅ Manual testing confirms all features work
✅ BidServiceCore can be safely deleted

## Next Steps

**Recommended Approach:**
Start with BidNotificationService (smallest, only 12 methods) to establish the pattern and build confidence, then proceed incrementally.

**Alternative Approach:**
If time is critical, keep current state where services call public BidServiceCore methods, and migrate incrementally over multiple sprints.
