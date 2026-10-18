using UnityEngine;
namespace ScaredyCat
{
    public sealed class MechanicsLabControls : MonoBehaviour
    {
        public GameFlow flow;
        public PawAttack attack;
        public MischiefProp[] props;
        public Transform[] stations;
        float charge = 1;
        int debugReward;
        void OnGUI()
        {
            if (!flow.DebugVisible) return;
            GUILayout.BeginArea(new Rect(20, 110, 350, 650), GUI.skin.box);
            GUILayout.Label("SCAREDY CAT / TEST CONTROLS");
            GUILayout.Label($"Agro {flow.agro.Ledger.Value:0}  |  Grounded {flow.cat.Grounded}");
            GUILayout.Label($"Owner: {(flow.owner ? flow.owner.State.ToString() : "none")}");
            GUILayout.Label($"Charge: {charge:P0}"); charge = GUILayout.HorizontalSlider(charge, 0, 1);
            if (GUILayout.Button("Close panel and swat")) { flow.SetDebug(false); attack.DebugStrike(charge); }
            if (GUILayout.Button("Reduce agro by 10")) flow.agro.Report(new MischiefEvent(--debugReward, MischiefKind.Break, true));
            if (GUILayout.Button("Fracture first intact prop"))
                foreach (var prop in props) if (prop && !prop.Broken && prop.breakable)
                { prop.ReceiveHit(new HitContext(0, flow.cat.gameObject, prop.transform.position, Vector3.up, 1, 3)); break; }
            if (GUILayout.Button("Reset props (keep reward ledger)")) foreach (var prop in props) if (prop) prop.ResetProp();
            if (flow.owner && GUILayout.Button("Ragdoll owner"))
                flow.owner.ReceiveHit(new HitContext(0, flow.cat.gameObject, flow.owner.hips.position, Vector3.forward, charge, Mathf.Lerp(1.8f, 7, charge)));
            if (flow.owner && GUILayout.Button("Reset owner")) flow.owner.ResetOwner();
            if (stations != null) foreach (var station in stations)
                if (station && GUILayout.Button("Go to " + station.name)) { flow.cat.Teleport(station.position, station.eulerAngles.y); flow.SetDebug(false); }
            if (GUILayout.Button("Restart fresh session")) flow.Restart();
            if (GUILayout.Button("Close (F1)")) flow.SetDebug(false);
            GUILayout.EndArea();
        }
    }
}
