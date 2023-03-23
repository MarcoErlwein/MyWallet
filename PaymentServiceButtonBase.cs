using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StrommarMember.Mobile7.Components.PayPal
{
    public class PaymentServiceButtonBase : ComponentBase
    {
        [Inject] private IJSRuntime jsRuntime { get; set; }
        [Inject] public IConfiguration configuration { get; set; }

        [Parameter] public int type { get; set; }
        [Parameter] public string ContainerId { get; set; } = "paypal-button-container";
        [Parameter] public string OnApproveFunctionName { get; set; }
        [Parameter] public DotNetObjectReference<object> DotNetReference { get; set; }

        private string ClientId { get; set; }
        private bool CanRenderButtons { get; set; } = false;
        public bool IsLoading { get; set; } = true;

        protected override async Task OnInitializedAsync()
        {
            ClientId = configuration.GetSection("ConfiguracionPayment:PayPal:Client_Id").Value;
            //ContainerId += PlanId;
            ContainerId = ContainerId;
        }

        protected RenderFragment RenderPaypalSdk
        {
            get
            {

                RenderFragment form = b =>
                {
                    b.OpenElement(0, "script");
                    b.AddAttribute(0, "src", $"https://www.paypal.com/sdk/js" +
                        $"?client-id={this.ClientId}" +
                        $"&enable-funding=venmo" +
                        $"&currency=USD");
                    b.AddAttribute(0, "data-sdk-integration-source", "button-factory");
                    b.CloseElement();
                };

                this.CanRenderButtons = true;

                return form;
            }
        }

        private async Task RenderButtons()
        {

            if (DotNetReference == null)
            {
                object[] parameters = { $"#{this.ContainerId}", OnApproveFunctionName, this.type };
                await this.jsRuntime.InvokeVoidAsync("RenderPaypalOrderButton", parameters);
            }
            else
            {
                object[] parameters = { DotNetReference, $"#{this.ContainerId}", OnApproveFunctionName, this.type };
                await this.jsRuntime.InvokeVoidAsync("RenderPaypalOrderButtonReference", parameters);
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && IsLoading && CanRenderButtons)
            {
                await Task.Delay(5000);
                await RenderButtons();

                IsLoading = false;

                StateHasChanged();
            }

            await base.OnAfterRenderAsync(firstRender);
        }

    }
}
