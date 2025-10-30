using Nafes.CrossCutting.Common.OperationResponse;
using Nafes.CrossCutting.Common.Security;
using Nafes.CrossCutting.Model.Entities;
using Nafis.Services.Contracts;
using Nafis.Services.DTO.AssociationWithdraw;
using Nafis.Services.DTO.Bid;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tanafos.Main.Services.DTO.Bid;
using Nafes.CrossCutting.Data.Repository;
using Nafis.Services.Contracts.CommonServices;
using Microsoft.EntityFrameworkCore;
using Nafes.CrossCutting.Model.Enums;
using Nafes.CrossCutting.Common.API;

namespace Nafis.Services.Implementation
{

    public class BidStatisticsService : IBidStatisticsService
    {
        private readonly BidServiceCore _bidServiceCore;
        private readonly ICrossCuttingRepository<BidViewsLog, long> _bidViewsLogRepository;
        private readonly ICrossCuttingRepository<Organization, long> _organizatioRepository;
        private readonly ICrossCuttingRepository<Bid, long> _bidRepository;
        private readonly IDateTimeZone _dateTimeZone;
        private readonly ICurrentUserService _currentUserService;

        public BidStatisticsService(
            BidServiceCore bidServiceCore,
            ICrossCuttingRepository<BidViewsLog, long> bidViewsLogRepository,
            ICrossCuttingRepository<Organization, long> organizatioRepository,
            ICrossCuttingRepository<Bid, long> bidRepository,
            IDateTimeZone dateTimeZone,
            ICurrentUserService currentUserService)
        {
            _bidServiceCore = bidServiceCore;
            _bidViewsLogRepository = bidViewsLogRepository;
            _organizatioRepository = organizatioRepository;
            _bidRepository = bidRepository;
            _dateTimeZone = dateTimeZone;
            _currentUserService = currentUserService;
        }

        public async Task<OperationResult<long>> IncreaseBidViewCount(long bidId)
        {
            var bid = await _bidRepository.FindByIdAsync(bidId);
            if (bid == null)
                return OperationResult<long>.Fail(HttpErrorCode.NotFound, CommonErrorCodes.BID_NOT_FOUND);

            var user = _currentUserService.CurrentUser;
            return await IncreaseBidViewCountNew(bid, user);
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


    }
}
