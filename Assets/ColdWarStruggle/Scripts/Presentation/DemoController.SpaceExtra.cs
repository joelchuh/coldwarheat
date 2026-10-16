using System.Threading.Tasks;
using ColdWarStruggle.Application;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoController
    {
        public bool CanDeclineExtraActions => ReadView().Practice?.CanDeclineExtraActions==true;
        public Task<CardActionResult> DeclineExtraActionsAsync()=>SendCard(CardActionKind.DeclineExtraActions);
    }
}
