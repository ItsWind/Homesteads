using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace Homesteads.MissionLogics {
    public class HomesteadConversationMissionLogic : MissionLogic {
        public override void OnAgentInteraction(Agent userAgent, Agent agent) {
            if (!agent.IsHuman || !userAgent.IsHuman)
                return;
            agent.SetLookAgent(userAgent);
            agent.SetLookToPointOfInterest(userAgent.GetEyeGlobalPosition());
            Campaign.Current.ConversationManager.ConversationEndOneShot += () => {
                agent.SetLookAgent(null);
                agent.SetLookToPointOfInterest(Vec3.Invalid);
            };
        }
    }
}
