﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OID.DataProvider.Interfaces;
using OID.DataProvider.Models;
using OID.DataProvider.Models.Deal;
using OID.DataProvider.Models.Deal.In;
using OID.DataProvider.Models.User;
using OID.Web.Core;
using OID.Web.Models;
using OID.Web.Models.Deal;
using OID.Web.Models.Partials;
using OID.Web.Models.User;


namespace OID.Web.Controllers
{
    [Authorize("HasSessionID")]
    public class DealController : OIDController
    {
        private readonly IDealProvider _dealProvider;
        private readonly IMapper _mapper;
        private readonly IDealObjectProvider _dealObjectProvider;
        private readonly IUserProvider _userProvider;
        private readonly IRegionProvider _regionProvider;

        public DealController(IDealProvider dealProvider, IMapper mapper, IDealObjectProvider dealObjectProvider, IUserProvider userProvider, IRegionProvider regionProvider)
        {
            _dealProvider = dealProvider;
            _mapper = mapper;
            _dealObjectProvider = dealObjectProvider;
            _userProvider = userProvider;
            _regionProvider = regionProvider;
        }

        public async Task<IActionResult> Index()
        {
            var deals = await _dealProvider.GetDeals();
            if (deals.ResultMessage.MessageType != MessageType.Error)
            {
                var model = _mapper.Map<List<ItemDealViewModel>>(deals.Model);
                return View(model);
            }
            else
            {
                ShowError(deals.ResultMessage.Message);
            }

            return RedirectToIndex();
        }

        public async Task<IActionResult> UpdateBuy(int dealId)
        {
            var dealModifyModel = await CreateUpdateDealModel(dealId);

            var model = new UpdateBuyDealViewModel
            {
                DealId = dealId,
                DealModel = dealModifyModel
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBuy(int dealId, DealModifyModel modfyModel)
        {
            var currentDeliveryObjectsTask = _dealObjectProvider.GetDealObjects(dealId);

            var currentDeliveryObjects = (await currentDeliveryObjectsTask).Model;

            var objectsToDelete = currentDeliveryObjects
                .Where(c => modfyModel.SelectedDealObjects.All(s => s.ObjectId != c.ObjectId))
                .Select(o => o.DealObjectId)
                .ToList();

            var objectsToAdd = modfyModel.SelectedDealObjects
                .Where(s => currentDeliveryObjects.All(c => c.ObjectId != s.ObjectId))
                .Select(o => o.ObjectId)
                .ToList();

            var updateModel = new DealUpdateModel(dealId, objectsToDelete, objectsToAdd, modfyModel.Price, modfyModel.Comment, null, modfyModel.DeleveryTypeId,
                modfyModel.Size, modfyModel.Weight, modfyModel.AddressModel.City.CityCode, modfyModel.AddressModel.Locality.LocalityCode,
                modfyModel.AddressModel.RegionCode, modfyModel.AddressModel.Address, (DeleveryLocationType) modfyModel.AddressModel.DeliveryLocationType);

            await _dealProvider.UpdateDeal(updateModel, DealType.Buy);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> CreateBuy()
        {
            var model = await CreateDealModifyModel(DealType.Buy);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateBuy(DealModifyModel model)
        {
            var objectIdsToAdd = model.SelectedDealObjects
                .Select(o => o.ObjectId)
                .ToList();

            var createModel = new DealCreateModel(model.Price, model.Comment, null, model.DeleveryTypeId, model.Size,
                model.Weight, model.AddressModel.City.CityCode, model.AddressModel.Locality.LocalityCode, model.AddressModel.RegionCode,
                model.AddressModel.Address, (DeleveryLocationType) model.AddressModel.DeliveryLocationType, objectIdsToAdd);

            await _dealProvider.CreateDeal(createModel, DealType.Buy);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> UpdateSell(int dealId)
        {
            var sellModifyModel = await CreateUpdateDealModel(dealId);

            var model = new UpdateSellDealViewModel
            {
                DealId = dealId,
                SellDealModel = sellModifyModel
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSell(int dealId, SellDealModifyViewModel modfyModel)
        {
            var currentDeliveryObjectsTask = _dealObjectProvider.GetDealObjects(dealId);
            var createUserAccountTask = Task.FromResult(new DataProviderModel<CreateUserAccountModel>(new ResultMessage(0, MessageType.Information, "")));
            if (modfyModel.AccountAction == AccountAction.New)
            {
                createUserAccountTask = _userProvider.CreateUserAccount(modfyModel.PaymentNumber.Value, modfyModel.PaymentType);
            }

            await Task.WhenAll(currentDeliveryObjectsTask, createUserAccountTask);

            var currentDeliveryObjects = currentDeliveryObjectsTask.Result.Model;
            var createUserAccount = createUserAccountTask.Result.Model;

            var objectsToDelete = currentDeliveryObjects
                .Where(c => modfyModel.SelectedDealObjects.All(s => s.ObjectId != c.ObjectId))
                .Select(o => o.DealObjectId)
                .ToList();

            var objectsToAdd = modfyModel.SelectedDealObjects
                .Where(s => currentDeliveryObjects.All(c => c.ObjectId != s.ObjectId))
                .Select(o => o.ObjectId)
                .ToList();

            var accountId = modfyModel.AccountAction == AccountAction.Existed ? modfyModel.UserAccountId.Value : createUserAccount.AccountId;

            var updateModel = new DealUpdateModel(dealId, objectsToDelete, objectsToAdd, modfyModel.Price, modfyModel.Comment, accountId, modfyModel.DeleveryTypeId,
                modfyModel.Size, modfyModel.Weight, modfyModel.AddressModel.City.CityCode, modfyModel.AddressModel.Locality.LocalityCode,
                modfyModel.AddressModel.RegionCode, modfyModel.AddressModel.Address, (DeleveryLocationType) modfyModel.AddressModel.DeliveryLocationType);

            await _dealProvider.UpdateDeal(updateModel, DealType.Sell);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> CreateSell()
        {
            var model = await CreateSellModel();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSell(SellDealModifyViewModel model)
        {
            var createUserAccountTask = Task.FromResult(new DataProviderModel<CreateUserAccountModel>(new ResultMessage(0, MessageType.Information, "")));
            if (model.AccountAction == AccountAction.New)
            {
                createUserAccountTask = _userProvider.CreateUserAccount(model.PaymentNumber.Value, model.PaymentType);
            }

            var createUserAccount = (await createUserAccountTask).Model;

            var accountId = model.AccountAction == AccountAction.Existed ? model.UserAccountId.Value : createUserAccount.AccountId;
            var objectIdsToAdd = model.SelectedDealObjects
                .Select(o => o.ObjectId)
                .ToList();

            var createModel = new DealCreateModel(model.Price, model.Comment, accountId, model.DeleveryTypeId, model.Size,
                model.Weight, model.AddressModel.City.CityCode, model.AddressModel.Locality.LocalityCode, model.AddressModel.RegionCode,
                model.AddressModel.Address, (DeleveryLocationType) model.AddressModel.DeliveryLocationType, objectIdsToAdd);

            await _dealProvider.CreateDeal(createModel, DealType.Sell);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ViewDeal(int dealId)
        {
            var dealTask = _dealProvider.GetDeal(dealId);
            var deliveryTask = _dealProvider.GetDealDelevery(dealId);
            var objectsTask = _dealObjectProvider.GetDealObjects(dealId);
            var paymentsTask = _dealProvider.GetDealPayments(dealId);
            var cheksTask = _dealObjectProvider.GetChecksFromDeal(dealId);

            await Task.WhenAll(dealTask, deliveryTask, objectsTask, paymentsTask, cheksTask);

            var deal = dealTask.Result.Model;
            var delivery = deliveryTask.Result.Model;
            var objects = objectsTask.Result.Model;
            var checks = cheksTask.Result.Model;

            var objectsWithChecks = new List<ViewDealModel.DealObject>();
            foreach (var dealObject in objects)
            {
                var dealChecks = new List<ViewDealModel.DealObject.ObjectCheck>();
                foreach (var check in checks.Where(ch => ch.ObjectId == dealObject.ObjectId))
                {
                    dealChecks.Add(new ViewDealModel.DealObject.ObjectCheck
                    {
                        Description = check.Task
                    });
                }

                objectsWithChecks.Add(new ViewDealModel.DealObject
                {
                    ObjectId = dealObject.ObjectId,
                    CheckListId = dealObject.CheckListId,
                    ObjectName = dealObject.ObjectName,
                    AcceptedByMe = dealObject.IsApprovedByMe,
                    AcceptedByPartner = dealObject.IsApprovedByPartner,
                    ObjectChecks = dealChecks,
                    DealObjectId = dealObject.DealObjectId
                });
            }

            var model = new ViewDealModel
            {
                Size = delivery.SizeDeclire,
                DealId = deal.DealId,
                Price = deal.Price,
                Weight = delivery.WeightDeclire,
                Comment = deal.Comment,
                BuyerName = delivery.BuyerUserName,
                SellerName = delivery.SellerUserName,
                DeliveryTypeName = delivery.DeliveryCptyServiceName,
                DealObjects = objectsWithChecks,
                AcceptedByMe = deal.IsApprovedByMe,
                AcceptedByPartner = deal.IsApprovedByParner,
                IsBlocked = deal.Blocked,
                DealType = deal.DealType,
                IsAccepted = deal.IsAccepted
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveObject(int dealObjectId, string returnUrl)
        {
            await _dealObjectProvider.Approve(dealObjectId);
            return LocalRedirect(returnUrl);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveDeal(int dealId, string returnUrl)
        {
            await _dealProvider.Approve(dealId);
            return LocalRedirect(returnUrl);
        }

        private async Task<DealModifyModel> CreateDealModifyModel(DealType dealType)
        {
            var freeObjectsTask = _dealObjectProvider.GetUserObjects(true, dealType);
            var userTask = _userProvider.GetUser();
            var regionsTask = _regionProvider.GetRegions();
            var deliveryTypesTask = _dealProvider.GetDeleveryTypes();

            await Task.WhenAll(freeObjectsTask, userTask, regionsTask, deliveryTypesTask);

            var user = userTask.Result.Model;

            string regionCode = String.IsNullOrEmpty(user.RegionCode) ? Constants.MOSCOW_KLADR : user.RegionCode;

            var locationTask = _regionProvider.GetLocalities(regionCode);
            var cityTask = _regionProvider.GetLocationsByRegion(regionCode);

            await Task.WhenAll(locationTask, cityTask);

            var freeObjects = freeObjectsTask.Result.Model;
            var locations = locationTask.Result.Model;
            var cities = cityTask.Result.Model;
            var regions = regionsTask.Result.Model;
            var deliveryTypes = deliveryTypesTask.Result.Model;

            var deleveryType = AddressType.City;
            if (user.DeleveryLocationType.HasValue)
            {
                deleveryType = (AddressType) user.DeleveryLocationType;
            }

            string cityCode = String.IsNullOrEmpty(user.CityCode) ? cities.First().Code : user.CityCode;
            string localityCode = String.IsNullOrEmpty(user.LocalityCode) ? locations.First().Code : user.LocalityCode;
            string locationCode = String.IsNullOrEmpty(user.CityCode) ? locations.First().Code : user.CityCode;

            return new DealModifyModel
            {
                AddressModel = new AddressViewModel(nameof(SellDealModifyViewModel.AddressModel))
                {
                    Address = user.Address,
                    DeliveryLocationType = deleveryType,
                    City = new CityViewModel
                    {
                        CityCode = cityCode,
                        CityList = new SelectList(cities.OrderBy(l => l.Name), "Code", "Name", cityCode)
                    },
                    Locality = new LocalityViewModel
                    {
                        LocalityList = new SelectList(locations.OrderBy(l => l.Name), "Code", "Name", localityCode),
                        LocalityCode = user.LocalityCode,
                        LocationList = new SelectList(locations.OrderBy(l => l.Name), "Code", "Name", locationCode),
                        LocationCode = user.CityCode
                    },
                    RegionCode = regionCode,
                    RegionList = new SelectList(regions.OrderBy(l => l.Name), "Code", "Name", regionCode),
                },
                DeleveryTypes = new SelectList(deliveryTypes, "DeliveryId", "Name"),
                FreeDealObjects = new SelectList(freeObjects, "ObjectId", "Name")
            };
        }

        private async Task<SellDealModifyViewModel> CreateSellModel()
        {
            var createModifyModelTask = CreateDealModifyModel(DealType.Sell);

            var accountsTask = _userProvider.GetUserAccounts();

            await Task.WhenAll(createModifyModelTask, accountsTask);

            var modifyModel = createModifyModelTask.Result;
            var accounts = accountsTask.Result.Model;

            var sellDealModel = _mapper.Map<SellDealModifyViewModel>(modifyModel);

            var accountsFormatted = accounts.Select(a => new SelectItem
            {
                Value = a.AccountId,
                Text = $"{a.PaymentService.GetHumanName()} {a.AccountNumber}"
            });

            sellDealModel.UserAccounts = new SelectList(accountsFormatted, nameof(SelectItem.Value), nameof(SelectItem.Text));

            return sellDealModel;
        }

        private async Task<SellDealModifyViewModel> CreateUpdateDealModel(int dealId)
        {
            var dealObjectsTask = _dealObjectProvider.GetDealObjects(dealId);
            var dealTask = _dealProvider.GetDeal(dealId);
            var dealDeliveryTask = _dealProvider.GetDealDelevery(dealId);
            var sellModifyModelTask = CreateSellModel();

            await Task.WhenAll(dealObjectsTask, dealObjectsTask, dealTask, sellModifyModelTask);

            var sellModifyModel = sellModifyModelTask.Result;
            var deal = dealTask.Result.Model;
            var dealDelevery = dealDeliveryTask.Result.Model;
            var dealObjects = dealObjectsTask.Result.Model;

            sellModifyModel.Comment = deal.Comment;
            sellModifyModel.Price = deal.Price;
            sellModifyModel.Size = dealDelevery.SizeDeclire;
            sellModifyModel.SelectedDealObjects = _mapper.Map<List<SelectedDealObject>>(dealObjects);
            sellModifyModel.Weight = dealDelevery.WeightDeclire;

            sellModifyModel.UserAccountId = deal.AccountId;
            if (deal.AccountId.HasValue)
            {
                sellModifyModel.AccountAction = AccountAction.Existed;

                foreach (var userAccount in sellModifyModel.UserAccounts)
                {
                    if (userAccount.Value == deal.AccountId.Value.ToString())
                    {
                        userAccount.Selected = true;
                    }
                }
            }

            sellModifyModel.DeleveryTypeId = dealDelevery.DeliveryCptyServiceId;
            foreach (var deleveryType in sellModifyModel.DeleveryTypes)
            {
                if (deleveryType.Value == dealDelevery.DeliveryCptyServiceId.ToString())
                {
                    deleveryType.Selected = true;
                }
            }
            return sellModifyModel;
        }
    }
}
