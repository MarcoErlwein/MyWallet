using BlazorCognosShared.DTOs.Artists;
using BlazorCognosShared.GeneralConfig;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using StrommarMember.Mobile7.Data.PayPal;
using StrommarMember.Mobile7.Helpers;
using StrommarMember.Mobile7.Helpers.Clicks;
using StrommarMember.Mobile7.Interfaces.GeneralConfig;
using StrommarMember.Mobile7.Interfaces.MyWallet;
using StrommarMember.Mobile7.Interfaces.Shared;
using StrommarMember.Mobile7.Interfaces.SocialMedia;
using StrommarMember.Mobile7.Resources.Languages.Pitch;
using StrommarMember.Mobile7.Services.Bloquear;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StrommarMember.Mobile7.Pages.MyWallet
{
    public class MyWalletBase : ComponentBase
    {
        [CascadingParameter(Name = "userMember")] protected strMemberDTO memberDTO { get; set; }

        #region Inject
        [Inject] IClicksDataService clicksDataService { get; set; }
        [Inject] IParameterDataService ParameterDataService { get; set; }
        [Inject] IMemberDataService MemberDataService { get; set; }
        [Inject] UserTimeZone TimeZoneService { get; set; }
        [Inject] BloquearService bloquearService { get; set; }

        [Inject] IMyWalletDataServices myWalletDataServices { get; set; }
        //[Inject] IStringLocalizer<RPitch> localizer { get; set; }
        #endregion

        #region Variables Globales
        public strMemberDTO _Member { get; set; } = new strMemberDTO();
        public string BalanceMember { get; set; }
        public string BalanceMemberSelected { get; set; }
        public List<Funds> FundsList { get; set; } = new List<Funds>();
        public int typeSelected { get; set; }
        public DotNetObjectReference<object> dotNetHelper { get; set; }
        
        public Func<ApprovedPayPalPaymentService, Task> SendActionInvoiceAsync;

        public bool IsSubmit { get; set; } = false;
        #endregion

        [JSInvokable("OnApprovedMyWalletPaymentService")]
        public async Task OnApprovedPaymentService(ApprovedPayPalPaymentService approvedPayPalPaymentService)
        {
            await SendActionInvoiceAsync.Invoke(approvedPayPalPaymentService);
        }

        protected override async Task OnInitializedAsync()
        {

            _ = InvokeAsync(async () =>
            {
                bloquearService.ShowBloquear();
                typeSelected = 0;
                await ObtenerBalance();
                await CargarTipoMontos();
                SendActionInvoiceAsync = SaveInvoice;
                dotNetHelper = DotNetObjectReference.Create<object>(this);
                bloquearService.HideBloquear();
            });
        }

        public async void btnCancelar()
        {
            typeSelected = 0;
            IsSubmit = false;
            await ObtenerBalance();
            await InvokeAsync(StateHasChanged);
        }
       
        public async void GenerarPaypal()
        {
            if (typeSelected != 0)
            {
                IsSubmit = true;
                await InvokeAsync(StateHasChanged);
            }
        }
        public async Task CargarTipoMontos()
        {
            try
            {

                string keyFunds = "FundsAmountType";
                var response = await ParameterDataService.GetByKey(keyFunds);
                if (response.Succeeded)
                {
                    var param = response.Data;
                    var fundsData = JsonConvert.DeserializeObject<List<Funds>>(param.parValue);
                    FundsList = fundsData.OrderBy(x => x.type).ToList();
                    //typeSelected = FundsList.FirstOrDefault().type;
                    await InvokeAsync(StateHasChanged);
                }
                var resultClicks = await clicksDataService.AddClicks(new strClicks
                {
                    memId = memberDTO.memId,
                    feaId = UtilsClicks.Features.MyWallet,
                    subFeatures = UtilsClicks.SubFeatures.MyWallet,
                    cliDateTime = await TimeZoneService.GetLocalDateTimeMobil(DateTime.UtcNow),
                    cliOrigen = UtilsClicks.Origen.OrigenMovil
                });

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public async Task ObtenerBalance()
        {
            var response = await MemberDataService.GetMemberBalance(memberDTO.memId);
            if (response.Succeeded)
            {
                _Member = response.Data;
                var valor = Math.Round((_Member.memBalanceCurator ?? 0), 2).ToString("N2");
                if (string.IsNullOrEmpty(_Member.curId))
                {

                    BalanceMember = "USD" + " " + valor;
                }
                else
                {
                    BalanceMember = _Member.curId + " " + valor;
                }
            }
        }

        public async void ChangeAmount(int type)
        {
            typeSelected = type;
            IsSubmit = false;
            var valor = Math.Round(((_Member.memBalanceCurator + typeSelected) ?? 0), 2).ToString("N2");
            if (string.IsNullOrEmpty(_Member.curId))
            {

                BalanceMemberSelected = "USD" + " " + valor;
            }
            else
            {
                BalanceMemberSelected = _Member.curId + " " + valor;
            }
            await InvokeAsync(StateHasChanged);
        }

        public async Task SaveInvoice(ApprovedPayPalPaymentService data)
        {           

            _ = InvokeAsync(async () =>
            {
                bloquearService.ShowBloquear();
                var invoice = new strExtraBoostMovilDTO
                {
                    curId = memberDTO.curId,
                    extDate = DateTime.UtcNow,
                    extStatus = "A",
                    memId = memberDTO.memId,
                    extValue = typeSelected,
                    intId = 2,
                    //extInvoiceId = data.subscriptionID,
                    // extInvoiceStatus = "PAID"
                    extInvoiceId = data.orderID + "," + data.transaccionID,
                    extInvoiceStatus = string.IsNullOrEmpty(data.transaccionID) == true ? "IMCOMPLETED" : "PAID"
                };

                var response = await myWalletDataServices.AddFunds(invoice);
                
                    var resultClicks = await clicksDataService.AddClicks(new strClicks
                    {
                        memId = memberDTO.memId,
                        feaId = UtilsClicks.Features.MyWallet,
                        subFeatures = UtilsClicks.SubFeatures.MyWalletAddFunds,
                        cliDateTime = await TimeZoneService.GetLocalDateTimeMobil(DateTime.UtcNow),
                        cliOrigen = UtilsClicks.Origen.OrigenMovil
                    });
                

                if (!string.IsNullOrEmpty(data.transaccionID))
                {
                    if (response.Succeeded)
                    {
                        //memberDTO.memBalanceCurator += FundsSelected.type;
                        typeSelected = 0;;
                        //StateHasChanged();
                        //isSending = false;
                        StateHasChanged();

                    }
                }
                BalanceMember = BalanceMemberSelected;
                await InvokeAsync(StateHasChanged);
                bloquearService.HideBloquear();
            });

            

            //var resultClicks = await clicksDataService.AddClicks(new strClicks
            //{
            //    memId = memberDTO.memId,
            //    feaId = UtilsClicks.Features.MyWallet,
            //    subFeatures = UtilsClicks.SubFeatures.MyWalletAddFunds,
            //    cliDateTime = await TimeZoneService.GetLocalDateTime(DateTime.UtcNow)
            //});



            

        }
    }
    public class Funds
    {
        public int type { get; set; }
        public string planId { get; set; }
    }
}