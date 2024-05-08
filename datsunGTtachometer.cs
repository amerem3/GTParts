using HutongGames.PlayMaker;
using MSCLoader;
using UnityEngine;
using System.Linq;
using HutongGames.PlayMaker.Actions;
using System;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using System.Reflection;
using System.Text;

namespace GTParts
{
	public class LightController : MonoBehaviour
	{
		public Light light;
		public float range = 1;
		public LightShadows shadows = LightShadows.None;
		public float currentMod = 5;
		public float currentModAfter = 5;
		public float intensityMult = 1;
		public Color colorSetting = new Color(1, 1, 1);
		public float targetHeat = 3600;
		public float thermalMass = 0.0002f;
		public float disapation = 0.00105f;
		public bool lightOn;
		public bool useTotal;
		public Color color;
		public float voltage;
		public float intensity;
		public float heat = 300;
		public float resistance = 5;
		public float wear = 100;
		public bool damaged;
		public bool blowing;
		public float wearBlowMultiplier = 2;
		public float wearMultiplier = 0.1f;
		public float spotAngle;
		public void Start()
		{
			if (gameObject.GetComponent<Light>())
				light = gameObject.GetComponent<Light>();
			else
				light = gameObject.AddComponent<Light>();

			light.intensity = 0;
			light.range = range;
			light.shadows = shadows;
		}

		public void FixedUpdate()
		{
			resistance = Mathf.Clamp(1 / Mathf.Log((targetHeat / 10f / heat) + 1) * currentMod, 1, 1000);
			voltage = (!damaged && lightOn ? (useTotal ? GlobalVariables.lightingVolts : GlobalVariables.volts) : 0) * (resistance / (4 + resistance));
   
			heat = Mathf.Clamp(heat, 300, targetHeat * 1.5f);
			float wantedTemp = Mathf.Pow(voltage, 2) / resistance;
			heat += wantedTemp / thermalMass * Time.fixedDeltaTime;
			heat -= (heat - 300) / thermalMass * disapation * Time.fixedDeltaTime;

			if (!blowing)
				currentModAfter = currentMod;
			else
				currentModAfter *= Time.fixedDeltaTime;
    
			wear -= Mathf.Clamp(Mathf.Pow(heat - targetHeat / 1.2f, wearBlowMultiplier) * wearMultiplier, 0, 5);
			wear = Mathf.Clamp(wear, 0, 100);
			if (heat > targetHeat * 2f)
			{
				// damaged = true;
				blowing = false;
			}
   
			if (wear <= 0)
			{
				//   damaged = true;
				blowing = false;
			}
		}

		public void Update()
		{
			color = CalculateColor(heat) * colorSetting;
			intensity = CalculateIntensity(heat, color) ;

			SetLight(ref light, color * intensityMult * (wear / 200f + 0.5f), intensity);
        }

		public static Color CalculateColor(float temperture)
		{
			float redVal;
			float greenVal;
			float blueVal;
			float tempDivHundred = temperture / 100f;

			if (tempDivHundred <= 66f)
			{
				blueVal = tempDivHundred <= 19 ? 0 : 138.5177312231f * Mathf.Log(tempDivHundred - 10) - 305.0447927307f;
				redVal = 255;
				greenVal = 99.4708025861f * Mathf.Log(tempDivHundred) - 161.1195681661f;
			}
			else
			{
				blueVal = 255;
				redVal = 329.698727446f * (Mathf.Pow(tempDivHundred - 60, -0.1332047592f));
				greenVal = 288.1221695283f * (Mathf.Pow(tempDivHundred - 60, -0.0755148492f));
			}

			Color color = new Color(Mathf.Clamp(redVal, 0, 255) / 255f, Mathf.Clamp(greenVal, 0, 255) / 255f, Mathf.Clamp(blueVal, 0, 255) / 255f);
			return color;
		}
		public static float CalculateIntensity(float temperture, Color tempColor)
		{
			float brightness = Mathf.Pow(temperture, 4) / 78000000000000f * (1 / (tempColor.r + tempColor.b + tempColor.g / 3f));
			float intensity = GTParts.brightness * brightness;
			return intensity;
		}
		public static void SetLight(ref Light targetLight, Color color, float intensity)
		{
			targetLight.color = color;
			targetLight.intensity = intensity;
		}
	}
	
	public class FUEL : MonoBehaviour
	{
		public bool active;

		public FsmFloat fuelValue;
		public float fuelGaugeNeedleValue;
		public Transform needleTransform;

		public float downMult = 2.7f;
		public float downPow = 1.2f;
		public float upMult = 13f;
		public float upPow = 1.2f;

		private readonly Vector3 maxRotation = new Vector3(0, -10.5f, 0);
		private readonly Vector3 minRotation = new Vector3(0, -105f, 0);
		private readonly float maxLevel = 36;

		public LightController fuelController;
		public bool fuelOn;
		public float fuelTreshhold = 5;
		public LightController lightController;

		public void Update()
		{
			active = GlobalVariables.gtDashInstalled && GlobalVariables.dashWired;
			float fuel = active && GlobalVariables.fuelWired ? fuelValue.Value : 0;

			upMult = Mathf.Pow(GlobalVariables.volts, 1.4f) / 3;
			if (fuel > fuelGaugeNeedleValue)
				fuelGaugeNeedleValue += Mathf.Pow(Mathf.Clamp(fuel - fuelGaugeNeedleValue, 0, 30), upPow) * upMult / 3 * Time.deltaTime;
			fuelGaugeNeedleValue -= Mathf.Pow(Mathf.Clamp(fuelGaugeNeedleValue, 0, 5), downPow) * downMult * Time.deltaTime;

			needleTransform.localEulerAngles = Vector3.Lerp(maxRotation, minRotation, fuelGaugeNeedleValue / maxLevel);

			fuelOn = active && GlobalVariables.fuelWired && fuelValue.Value < fuelTreshhold;

			fuelController.lightOn = fuelOn;
			lightController.lightOn = active && GlobalVariables.gabariti;
		}

		public void Start()
		{
			Transform fuelGauge = gameObject.transform;
			fuelGauge.GetChild(0).localScale = new Vector3(0.8f, 1, 0.75f);
			fuelGauge.localPosition = new Vector3(0.273f, 0.584f, 0.817f);
			fuelGauge.localEulerAngles = new Vector3(350f, 57f, 0);

			fuelValue = GameObject.Find("Database/DatabaseMechanics/FuelTank").GetComponent<PlayMakerFSM>().FsmVariables.FindFsmFloat("FuelLevel");

			Destroy(fuelGauge.gameObject.GetComponent<PlayMakerFSM>());

			needleTransform = transform.GetChild(0);

			GameObject lightObject = new GameObject("FueLight");
			lightObject.transform.parent = transform;
			lightObject.transform.localPosition = new Vector3(0.0025f, -0.009f, 0.012f);
			fuelController = lightObject.AddComponent<LightController>();
			fuelController.colorSetting = new Color(18, 13, 0);
			fuelController.intensityMult = 2.8f;
			fuelController.shadows = LightShadows.Soft;
			fuelController.range = 0.1f;

			lightObject = new GameObject("GaugeLight");
			lightObject.transform.parent = transform;
			lightObject.transform.localPosition = new Vector3(-0.025f, -0.005f, 0.017f);
			lightController = lightObject.AddComponent<LightController>();
			lightController.colorSetting = new Color(10, 10, 10);
			lightController.intensityMult = 4f;
			lightController.shadows = LightShadows.Soft;
			lightController.range = 0.1f;
			lightController.useTotal = true;
		}
	}
	public class TEMP : MonoBehaviour
	{
		public bool active;

		public FsmFloat tempValue;
		public float tempGaugeNeedleValue;
		public Transform needleTransform;
		public float downMult = 2.7f;
		public float downPow = 1.2f;
		public float upMult = 13f;
		public float upPow = 1.2f;

		private readonly Vector3 maxRotation = new Vector3(0, -30f, 0);
		private readonly Vector3 minRotation = new Vector3(0, -130, 0);
		private readonly float maxLevel = 120f;
		public LightController lightController;

		public void Update()
		{
			active = GlobalVariables.gtDashInstalled && GlobalVariables.dashWired;

			float temp = active ? tempValue.Value : 0;

			upMult = Mathf.Pow(GlobalVariables.volts, 1.4f) / 3;
			if (temp > tempGaugeNeedleValue)
				tempGaugeNeedleValue += Mathf.Pow(Mathf.Clamp(temp - tempGaugeNeedleValue, 0, 30), upPow) * upMult / 3 * Time.deltaTime;
			tempGaugeNeedleValue -= Mathf.Pow(Mathf.Clamp(tempGaugeNeedleValue, 0, 5), downPow) * downMult * Time.deltaTime;

			needleTransform.localEulerAngles = Vector3.Lerp(maxRotation, minRotation, Mathf.Clamp((tempGaugeNeedleValue - 35f) / maxLevel, 0, 1));
			lightController.lightOn = active && GlobalVariables.gabariti;
		}

		public void Start()
		{
			Transform tempGauge = gameObject.transform;
			tempGauge.GetChild(0).localScale = new Vector3(0.8f, 1, 0.75f);
			tempGauge.localPosition = new Vector3(0.273f, 0.596f, 0.8725f);
			tempGauge.localEulerAngles = new Vector3(350f, 80f, 0);

			tempValue = GTParts.satsuma.transform.GetChild(15).GetChild(2).GetChild(1).gameObject.GetComponent<PlayMakerFSM>().FsmVariables.FindFsmFloat("CoolantTemp");

			Destroy(tempGauge.gameObject.GetComponent<PlayMakerFSM>());

			needleTransform = transform.GetChild(0);

			GameObject lightObject = new GameObject("GaugeLight");
			lightObject.transform.parent = transform;
			lightObject.transform.localPosition = new Vector3(-0.03f, -0.005f, 0.005f);
			lightController = lightObject.AddComponent<LightController>();
			lightController.colorSetting = new Color(10, 10, 10);
			lightController.intensityMult = 4f;
			lightController.shadows = LightShadows.Soft;
			lightController.range = 0.1f;
			lightController.useTotal = true;
		}
	}
	public class SPEEDO : MonoBehaviour
	{
		public bool active;

		private Drivetrain drivetrain;
		public float speedValue;
		public float speedGaugeNeedleValue;
		public Transform needleTransform;
		public float downMult = 25f;
		public float downPow = 1.2f;
		public float upMult = 60f;
		public float upPow = 1f;

		private readonly Vector3 maxRotation = new Vector3(0, 5f, 0);
		private readonly Vector3 minRotation = new Vector3(0, -350f, 0);
		public float maxLevel = 200f;
		public LightController gabaritController;
		public bool gabaritOn;
		public LightController chokeController;
		public bool chokeOn;
		public LightController longController;
		public bool longOn;
		public LightController lightController;
		private float speed;
		public void Update()
		{
			active = GlobalVariables.gtDashInstalled;

			speed = active ? drivetrain.differentialSpeed : 0;

			needleTransform.localEulerAngles = Vector3.Lerp(maxRotation, minRotation, Mathf.Clamp(speedGaugeNeedleValue / maxLevel, 0, 1));

			gabaritOn = active && GlobalVariables.gabariti && GlobalVariables.dashWired;
			chokeOn = active && GlobalVariables.choke && GlobalVariables.dashWired;
			longOn = active && GlobalVariables.longBeams && GlobalVariables.dashWired;
			gabaritController.lightOn = gabaritOn;
			chokeController.lightOn = chokeOn;
			longController.lightOn = longOn;
			lightController.lightOn = active && GlobalVariables.gabariti && GlobalVariables.dashWired;
		}
		public void FixedUpdate()
        {
			if (speed > speedGaugeNeedleValue)
				speedGaugeNeedleValue += Mathf.Pow(Mathf.Clamp(speed - speedGaugeNeedleValue, 0, 30), upPow) * upMult / 3f * Time.fixedDeltaTime;
			speedGaugeNeedleValue -= Mathf.Pow(Mathf.Clamp(speedGaugeNeedleValue, 0, 5), downPow) * downMult * Time.fixedDeltaTime;
		}
		public void Start()
		{
			Transform speedGauge = gameObject.transform;

			drivetrain = GlobalVariables.satsuma.GetComponent<Drivetrain>();

			Destroy(speedGauge.gameObject.GetComponent<PlayMakerFSM>());

			GameObject lightObject = new GameObject("GabarituLight");
			lightObject.transform.parent = transform;
			lightObject.transform.localPosition = new Vector3(0.035f, -0.009f, -0.017f);
			gabaritController = lightObject.AddComponent<LightController>();
			gabaritController.colorSetting = new Color(1, 10, 1);
			gabaritController.intensityMult = 1.7f;
			gabaritController.shadows = LightShadows.Soft;
			gabaritController.range = 0.03f;

			lightObject = new GameObject("ChokeLight");
			lightObject.transform.parent = transform;
			lightObject.transform.localPosition = new Vector3(0.028f, -0.009f, -0.028f);
			chokeController = lightObject.AddComponent<LightController>();
			chokeController.colorSetting = new Color(18, 13, 0);
			chokeController.intensityMult = 2f;
			chokeController.shadows = LightShadows.Soft;
			chokeController.range = 0.03f;

			lightObject = new GameObject("LongLight");
			lightObject.transform.parent = transform;
			lightObject.transform.localPosition = new Vector3(0.017f, -0.009f, -0.035f);
			longController = lightObject.AddComponent<LightController>();
			longController.colorSetting = new Color(1, 1, 20);
			longController.intensityMult = 5f;
			longController.shadows = LightShadows.Soft;
			longController.range = 0.03f;

			lightObject = new GameObject("GaugeLight");
			lightObject.transform.parent = transform;
			lightObject.transform.localPosition = new Vector3(-0.038f, -0.03f, 0.035f);
			lightController = lightObject.AddComponent<LightController>();
			lightController.colorSetting = new Color(10, 10, 10);
			lightController.intensityMult = 3f;
			lightController.shadows = LightShadows.Soft;
			lightController.range = 0.2f;
			lightController.useTotal = true;

			needleTransform = transform.GetChild(0);
		}
	}
	public class TACHO : MonoBehaviour
	{
		public bool active;

		private Drivetrain drivetrain;
		public float rpmValue;
		public float rpmGaugeNeedleValue;
		public Transform needleTransform;
		public float downMult = 60f;
		public float downPow = 1.2f;
		public float upMult = 150f;
		public float upPow = 1f;

		private readonly Vector3 maxRotation = new Vector3(0, 0.2f, -184);
		private readonly Vector3 minRotation = new Vector3(0, 302f, -184);
		public float maxLevel = 302f;

		public LightController oilController;
		public bool oilOn;
		public LightController batteryController;
		public bool batteryOn;
		public LightController lightController;
		public readonly float multiplier = 36.1111f;
		private float rpm;
		public void Update()
		{
			active = GlobalVariables.gtTachInstalled && GlobalVariables.dashWired;

			rpm = active ? drivetrain.rpm / multiplier : 0;

			needleTransform.localEulerAngles = Vector3.Lerp(maxRotation, minRotation, Mathf.Clamp(rpmGaugeNeedleValue / maxLevel, 0, 1));

			oilOn = active && GlobalVariables.oil;
			batteryOn = active && GlobalVariables.battery;
			oilController.lightOn = oilOn;
			batteryController.lightOn = batteryOn;
			lightController.lightOn = active && GlobalVariables.gabariti;
		}
		public void FixedUpdate()
        {
			if (rpm > rpmGaugeNeedleValue)
				rpmGaugeNeedleValue += Mathf.Pow(Mathf.Clamp(rpm - rpmGaugeNeedleValue, 0, 30), upPow) * upMult / 3f * Time.fixedDeltaTime;
			rpmGaugeNeedleValue -= Mathf.Pow(Mathf.Clamp(rpmGaugeNeedleValue, 0, 5), downPow) * downMult * Time.fixedDeltaTime;
		}

		public void Start()
		{
			drivetrain = GlobalVariables.satsuma.GetComponent<Drivetrain>();

			Destroy(gameObject.transform.GetComponent<PlayMakerFSM>());
			needleTransform = transform.GetChild(0);

			GameObject lightObject = new GameObject("OilLight");
			lightObject.transform.parent = transform.parent;
			lightObject.transform.localPosition = new Vector3(0.014f, -0.021f, -0.032f);
			oilController = lightObject.AddComponent<LightController>();
			oilController.colorSetting = new Color(10, 1, 1);
			oilController.intensityMult = 0.5f;
			oilController.shadows = LightShadows.Soft;
			oilController.range = 0.1f;
   
			lightObject = new GameObject("BatteryLight");
			lightObject.transform.parent = transform.parent;
			lightObject.transform.localPosition = new Vector3(-0.008f, -0.021f, -0.032f);
			batteryController = lightObject.AddComponent<LightController>();
			batteryController.colorSetting = new Color(10, 1, 1); 
			batteryController.intensityMult = 0.5f;
			batteryController.shadows = LightShadows.Soft;
			batteryController.range = 0.1f;
   
			lightObject = new GameObject("GaugeLight");
			lightObject.transform.parent = transform.parent;
			lightObject.transform.localPosition = new Vector3(0, -0.02f, 0.043f);
			lightController = lightObject.AddComponent<LightController>();
			lightController.colorSetting = new Color(10, 10, 10);
			lightController.intensityMult = 5f;
			lightController.shadows = LightShadows.Soft;
			lightController.range = 0.2f;
			lightController.useTotal = true;
		}
	}

	public class LightingManager : MonoBehaviour
	{
		public static FUEL fuelClass;
		public static TEMP tempClass;
		public static SPEEDO speedoClass;
		public static TACHO tachoClass;

		public void Start()
		{
			Transform gauges = GlobalVariables.gtPanel.transform.GetChild(3);

			fuelClass = gauges.GetChild(0).gameObject.AddComponent<FUEL>();
			tempClass = gauges.GetChild(1).gameObject.AddComponent<TEMP>();
			speedoClass = gauges.GetChild(3).gameObject.AddComponent<SPEEDO>();

			tachoClass = GlobalVariables.gtTachometer.transform.GetChild(3).gameObject.AddComponent<TACHO>();
		}
		public void Update()
		{
			float resistance = 0;

			if (fuelClass.active && !fuelClass.lightController.damaged)
				resistance += 1 / fuelClass.lightController.resistance;
			if (tempClass.active && !tempClass.lightController.damaged)
				resistance += 1 / tempClass.lightController.resistance;
			if (speedoClass.active && !speedoClass.lightController.damaged)
				resistance += 1 / speedoClass.lightController.resistance;
			if (tachoClass.active && !tachoClass.lightController.damaged)
				resistance += 1 / tachoClass.lightController.resistance;

			resistance = Mathf.Clamp(1 / resistance, 0.1f, 1000);

			GlobalVariables.lightingVolts = (GlobalVariables.volts * resistance) / (GTParts.brightnessPotResistane + resistance);
		}
	}

	public class Status : MonoBehaviour
    {
		public PlayMakerFSM electricitySimulation;
		public FsmFloat voltsFsm;
		public FsmFloat oilPressure;
		public FsmFloat charging;

		public FsmBool stockInstalled;
		public FsmBool gtInstalled;

		public FsmBool dashWired;
		public FsmBool dashboardInstalled;

		public FsmBool fuelTankInstalled;
		public FsmBool fuelTankWired;

		public FsmFloat chokeValue;

		public GameObject rearLights;
		public GameObject longBeams;

		public FsmBool tachGtInstalled;
		public FsmBool tachInstalled;

		public void Update()
		{
            if (dashboardInstalled.Value)
            {
				GlobalVariables.dashWired = dashWired.Value;
				GlobalVariables.dashInstalled = stockInstalled.Value;
				GlobalVariables.gtDashInstalled =  gtInstalled.Value;
				GlobalVariables.fuelWired = fuelTankWired.Value && fuelTankInstalled.Value;
				GlobalVariables.choke = chokeValue.Value < -0.0001f;
				GlobalVariables.gabariti = rearLights.activeSelf;
				GlobalVariables.longBeams = longBeams.activeSelf;
				GlobalVariables.volts = voltsFsm.Value;
				GlobalVariables.oil = oilPressure.Value < 0.8f;
				GlobalVariables.battery = charging.Value < 0.03f;
				GlobalVariables.gtTachInstalled = tachInstalled.Value && (tachGtInstalled.Value && gtInstalled.Value || (!tachGtInstalled.Value && stockInstalled.Value));
			}
            else
            {
				GlobalVariables.dashWired = false;
				GlobalVariables.dashInstalled = false;
				GlobalVariables.gtDashInstalled = false;
			}
		}
		public void Start()
        {
			GlobalVariables.satsuma = gameObject;

			electricitySimulation = transform.GetChild(15).GetChild(2).GetChild(7).gameObject.GetComponent<PlayMakerFSM>();

			voltsFsm = electricitySimulation.FsmVariables.FindFsmFloat("Volts");
			charging = electricitySimulation.FsmVariables.FindFsmFloat("Charging");
			oilPressure = transform.GetChild(15).GetChild(1).GetChild(4).gameObject.GetComponents<PlayMakerFSM>()[1].FsmVariables.FindFsmFloat("OilPressureBar");
			
			stockInstalled = GameObject.Find("Database/DatabaseMechanics/DashboardMeters").GetComponent<PlayMakerFSM>().FsmVariables.FindFsmBool("Installed"); 
			gtInstalled = GTParts.gtPanelInfo.GetComponent<PlayMakerFSM>().FsmVariables.FindFsmBool("Installed");

			dashWired = GameObject.Find("Database/DatabaseWiring/WiringDash1").GetComponent<PlayMakerFSM>().FsmVariables.FindFsmBool("Installed");
			dashboardInstalled = GameObject.Find("Database/DatabaseMechanics/Dashboard").GetComponent<PlayMakerFSM>().FsmVariables.FindFsmBool("Installed");

			fuelTankInstalled = GameObject.Find("Database/DatabaseMechanics/FuelTank").GetComponent<PlayMakerFSM>().FsmVariables.FindFsmBool("Installed");
			fuelTankWired = GameObject.Find("Database/DatabaseWiring/WiringFueltank").GetComponent<PlayMakerFSM>().FsmVariables.FindFsmBool("Installed");

			chokeValue = GTParts.gtPanel.transform.GetChild(4).GetChild(0).GetChild(3).gameObject.GetComponent<PlayMakerFSM>().FsmVariables.FindFsmFloat("KnobPos");

			Transform powerOn = GlobalVariables.satsuma.transform.GetChild(16).GetChild(0);
			rearLights = powerOn.GetChild(3).gameObject;
			longBeams = powerOn.GetChild(1).gameObject;

			tachGtInstalled = GTParts.gtTachInfo.GetComponent<PlayMakerFSM>().FsmVariables.FindFsmBool("InstalledGT");
			tachInstalled = GTParts.gtTachInfo.GetComponent<PlayMakerFSM>().FsmVariables.FindFsmBool("Installed");
		}
	}

	public static class GlobalVariables
    {
		public static float volts;
		public static float lightingVolts;
		public static GameObject satsuma;

		public static bool dashWired;
		public static bool gtDashInstalled;
		public static bool dashInstalled;
		public static bool fuelWired;

		public static bool longBeams;
		public static bool gabariti;
		public static bool choke;

		public static bool oil;
		public static bool battery;

		public static bool gtTachInstalled;

		public static GameObject gtTachometer;
		public static GameObject gtPanel;
    }

	public static class Extensions
	{
		/// <summary>
		/// Inserts thing into array, increasing the size of array by 1
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="array"></param>
		/// <param name="item"></param>
		/// <param name="placement">if not set, will add onto the end of array</param>
		/// <returns>Returns edited array</returns>
		public static T[] Insert<T>(this T[] array, T item, int placement = -1)
		{
			if (array == null)
				return new T[] { item };

			if (placement < 0)
				placement = array.Length;

			placement = Mathf.Clamp(placement, 0, array.Length);
			Array.Resize(ref array, array.Length + 1);
			for (int i = array.Length - 1; i > placement; i--)
				array[i] = array[i - 1];

			array[placement] = item;

			return array;
		}
		public static void FsmVoidHook(this FsmState state, MethodInfo method, object[] parameters, int placement = -1)
		{

			FsmStateAction[] actions = state.Actions;
			void action() => method.Invoke(method, parameters);

			FsmHookActionHere item = new FsmHookActionHere
			{
				hook = action,
				name = method.Name
			};

			state.Actions = actions.Insert(item, placement);
		}

		public static void FsmActionHook(this FsmState state, Action action, int placement = -1)
		{
			FsmStateAction[] actions = state.Actions;

			FsmHookActionHere item = new FsmHookActionHere
			{
				hook = action,
				name = action.GetType().FullName
			};

			state.Actions = actions.Insert(item, placement);
		}

		public static void FsmHooker(this FsmState state, MethodInfo method, object[] parameters = null, int placement = -1)
		{
			FsmStateAction[] actions = state.Actions;
			void action() => method.Invoke(method, parameters);

			FsmHookActionHere item = new FsmHookActionHere
			{
				hook = action,
				name = method.Name
			};

			state.Actions = actions.Insert(item, placement);
		}

	}

	public class FsmHookActionHere : FsmStateAction
	{
		public Action hook;
		public string name;
		public override void OnEnter()
		{
			hook?.Invoke();
			Finish();
		}
	}

	public class CreditBehaviour : MonoBehaviour
    {
		public void Update()
		{
			HandleText();

			mediumSlowUpdateTimer += Time.deltaTime;
			if (fontCountDown)
				timerFontResset += Time.deltaTime;


			if (mediumSlowUpdateTimer > 0.08f)
			{
				mediumSlowUpdateTimer = 0;
				MediumSlowUpdate();
			}
		}
		
		public static void RollCredits()
		{
			for (int i = 0; i < 6; i++)
			{
				theseSpacesE[i] = "                             ";
				theseSpacesL[i] = "                             ";
			}
			stringsDone = true;
			didCommand = true;
			fontSaved = false;
		}

		public readonly static string[] theseStringsE = new string[6]
	{
"███████╗",
"██╔════╝",
"█████╗  ",
"██╔══╝  ",
"███████╗",
"╚══════╝"
	};
		public static string[] theseSpacesE = new string[6]
	{
"",
"",
"",
"",
"",
""
	};
		public readonly static string[] theseStringsL = new string[6]
			{
"██╗     ",
"██║     ",
"██║     ",
"██║     ",
"███████╗",
"╚══════╝"
			};
		public static string[] theseSpacesL = new string[6]
	{
"",
"",
"",
"",
"",
""
	};
		public readonly static string[] theseStringsS = new string[6]
			{
"███████╗",
"██╔════╝",
"███████╗",
"╚════██║",
"███████║",
"╚══════╝"
			};
		public readonly static string[] theseStringsNum = new string[6]
		{
" ██╗██████╗ ██████╗ ",
"███║╚════██╗╚════██╗",
"╚██║ █████╔╝ █████╔╝",
" ██║██╔═══╝ ██╔═══╝ ",
" ██║███████╗███████╗",
" ╚═╝╚══════╝╚══════╝"
		};

		public static float mediumSlowUpdateTimer;
		public static bool didCommand;
		public static bool stringsDone;
		public static bool stringsDone2;
		public static bool fontSaved;
		public static float timerFontResset;
		public static bool fontCountDown;
		public static int currentStep;
		public static bool goingUp;

		public static string Rainbow(int numOfSteps, int step)
		{
			var r = 0.0;
			var g = 0.0;
			var b = 0.0;
			var h = (double)step / numOfSteps;
			var i = (int)(h * 6);
			var f = h * 6.0 - i;
			var q = 1 - f;

			switch (i % 6)
			{
				case 0:
					r = 1;
					g = f;
					b = 0;
					break;
				case 1:
					r = q;
					g = 1;
					b = 0;
					break;
				case 2:
					r = 0;
					g = 1;
					b = f;
					break;
				case 3:
					r = 0;
					g = q;
					b = 1;
					break;
				case 4:
					r = f;
					g = 0;
					b = 1;
					break;
				case 5:
					r = 1;
					g = 0;
					b = q;
					break;
			}

			return "#" + ((int)(r * 255)).ToString("X2") + ((int)(g * 255)).ToString("X2") + ((int)(b * 255)).ToString("X2") + "ff";
		}
		public static void HandleText()
		{
			if (GTParts.modLog.Length > 0)
			{
				if (stringsDone)
				{
					if (!fontSaved)
					{
						timerFontResset = 0f;
						fontSaved = true;
						ModConsole.console.logTextArea.fontSize = 10;
					}

				}

				if (stringsDone2)
				{
					stringsDone2 = stringsDone;
					ModConsole.console.inputField.text = "clear";
					ModConsole.console.runCommand();
				}
				else
				{
					if (timerFontResset > 1.5f)
					{
						fontCountDown = false;

						if (fontSaved)
						{
							fontSaved = false;
							ModConsole.ChangeFontSize();
							ModConsole.console.inputField.text = "clear";
							ModConsole.console.runCommand();
							ModConsole.Print("a");
						}
					}
					else
						fontCountDown = true;
				}

				ModConsole.Print(GTParts.ModString + GTParts.modLog + GTParts.DividerString());
				GTParts.modLog = "";
			}

			if (!stringsDone2)
			{
				if (timerFontResset > 3f)
				{
					fontCountDown = false;

					if (fontSaved)
					{
						fontSaved = false;
						ModConsole.ChangeFontSize();
						ModConsole.console.inputField.text = "clear";
						ModConsole.console.runCommand();
					}
				}
				else
					fontCountDown = true;
			}
		}

		public static void MediumSlowUpdate()
		{
			if (!stringsDone)
				return;
			stringsDone = didCommand;
			stringsDone2 = true;
			GTParts.modLog = "";
			string z = "";
			if (goingUp)
			{
				if (currentStep < 30)
					currentStep++;
				else
					goingUp = false;
			}
			else
			{
				if (currentStep > 0)
					currentStep--;
				else
					goingUp = true;
			}
			string hex = Rainbow(30, currentStep);

			string d = string.Format("<color={0}>", "#ccccccff");
			string b = string.Format("<color={0}>", hex);

			for (int i = 0; i < 6; i++)
			{
				string x = string.Format("{0}{1}{2}{3}{4}{5}", theseStringsE[i], theseSpacesE[i], theseStringsL[i], theseSpacesL[i], theseStringsS[i], !stringsDone ? theseStringsNum[i] : "");
				if (x.Length > 44)
					x = x.Remove(x.Length - (x.Length - 44));

				z += string.Format("\n{0}", x);
			}

			GTParts.modLog = z.Replace("█", string.Format("{0}█</color>", b)).Replace("═", string.Format("{0}═</color>", d)).Replace("║", string.Format("{0}║</color>", d)).Replace("╗", string.Format("{0}╗</color>", d)).Replace("╔", string.Format("{0}╔</color>", d)).Replace("╝", string.Format("{0}╝</color>", d)).Replace("╚", string.Format("{0}╚</color>", d)).Replace(" ", "<color=black>░</color>");

			for (int i = 0; i < 6; i++)
			{
				if (theseSpacesE[i].Length > 0)
					theseSpacesE[i] = theseSpacesE[i].Remove(theseSpacesE[i].Length - 1);
				else if (theseSpacesL[i].Length > 0)
					theseSpacesL[i] = theseSpacesL[i].Remove(theseSpacesL[i].Length - 1);
				else
					didCommand = false;
			}
		}
	}

	public class CreditCommand : ConsoleCommand
	{
		public override string Name => "credits";

		public static readonly string rainbowAmazement = "<i><color=pink>a</color><color=purple>m</color><color=blue>a</color><color=green>z</color><color=yellow>i</color><color=orange>n</color><color=red>g</color></i>";
		public override string Help => string.Format("Find out who made this {0} <b>'GTParts'</b> mod!", rainbowAmazement);
		public override void Run(string[] args)
		{
			CreditBehaviour.RollCredits();
		}
	}

	public class GTParts : Mod
	{
		public override string ID => "GTParts";
		public override string Name => "GT Parts";
		public override string Author => "ELS122";
		public override string Version => "1.2";
		public override bool UseAssetsFolder => true;

		public static string ModString;
		public static float repeatTimer;

		public static bool isSavingNormally1;
		public static bool isSavingNormally2;

		public static Drivetrain satsumaDrivetrain;

		public static bool debugMode;

		public static GameObject gtPanel;
		public static GameObject gtPanelInfo;
		public static GameObject gtPanelTrigger;
		public static Texture2D dashTexture;
		public static Transform gaugeTriggers;

		public static GameObject gtTachometer;
		public static GameObject gtTachInfo;
		public static GameObject gtTachTrigger;

		public static CreditBehaviour credits;

		public static GameObject stockTriggerMeters;

		public static string modLog = "";

		public static readonly float brightness = 5.7f;
		public static float brightnessPotResistane;
		public static readonly float potValue = 5f;
		public static GameObject lightPotentiometer;
		public static GameObject lightPotentiometerFsmObject;
		public static PlayMakerFSM lightPotentiometerFsm;

		public static GameObject satsuma;
		public static Transform warnings;
		public static readonly FloatClamp emptyAction = new FloatClamp
		{
			floatVariable = 0,
			minValue = 0,
			maxValue = 0
		};

		public static Texture2D needleTexture;
		public static Texture2D tachTexture;
		public static Texture2D odometerText;
		public static Texture2D odometerRedText;
		public static bool whiteDash = false;
		public static Settings dashChoice = new Settings("DashChoice", "Use alternate white dash", false, UpdateDashTexture);
		public static Settings ressetMod = new Settings("ResetParts", "Reset parts", ResetMod);
		public static bool disableMod;
		public static Transform radioTriggers;

		public static FsmBool dashInstalledStatus;
		public static FsmBool blinkerDashRequiredBool;
		public static Texture2D dashboardTexture;
		public static Texture2D switchTexture;
		public static Texture2D dashboardSurface;
		public static GameObject stockPanel;

		public static bool resetMod;

		public static GameObject cdSave;
		public static GameObject cd;
		public static GameObject cdTrigger;
		public static FsmBool cdPlayerPurchased;
		public static GameObject nosSave;
		public static GameObject nos;
		public static GameObject nosTrigger;
		public static bool resetOnLoad;
		public static FsmBool nosInsalledYes;
		public static Texture2D originalDashboardSurface;
		public static bool alreadyHappened;
		public static FsmInt selection;
		public static FsmInt selectionsFromStock;
		public static FsmInt selectionsFromGt;
		public static FsmBool nosPurchased;
		public static GameObject nosStockTrigger;
		public static GameObject nosObject;

		public override void ModSettings()
		{
			Settings.AddCheckBox(this, dashChoice);
			Settings.AddText(this, "");
			Settings.AddButton(this, ressetMod, "(will apply in game)");
		}
		public override void OnNewGame()
		{
			ResetMod();
		}
		public override void OnLoad()
		{
#if DEBUG
			debugMode = true;
#endif

			string typeLoader = !debugMode ? "" : "<color=orange>Debug</color>";
			ModString = string.Format("<color=green><b>{0}</b> (v{1})</color> {2}", Name, Version, typeLoader);
			ModString += DividerString();


            try
            {
                disableMod = ModLoader.IsModPresent("datsunGTtachometer") ? throw new Exception("<color=red>This mod isn't compatable with the previous GtTach mod, please remove the mod from the mod folder. : </color><color=red>Mod has been <b>DISABLED</b></color>") : false;

                if (disableMod) return;

                whiteDash = bool.Parse(dashChoice.GetValue().ToString());
                LoadTextures();

                satsuma = GameObject.Find("SATSUMA(557kg, 248)");
                satsumaDrivetrain = satsuma.GetComponent<Drivetrain>();
                credits = satsuma.AddComponent<CreditBehaviour>();
                ConsoleCommand.Add(new CreditCommand());
                LoadTach();

                LoadPanel();
                FixPartsThatGetInstalledOnGtPanel();
                AddBrightnessKnob();
                FixBlinkersForGtPanel();
                TaalasGaismas();
                ChangeDashTextures();

                CheckIfResset();

                RefrenceStuff();

                ChangeSomeGauges();
            }
			catch (Exception e)
			{
				disableMod = true;

				string error = string.Format("\n <color=red><b>Error:</b></color> <color=orange>{0}</color>\n <color=red><b>Source:</b></color> <color=orange>{1}</color>\n <color=red><b>Site:</b></color> <color=orange>{2}</color>\n <color=red><b>Trace:</b></color> <color=orange>{3}</color>", e.Message, e.Source, e.TargetSite, e.StackTrace);
				modLog += error.Replace("[GameObject]", "").Replace(" [0x00000] in <filename unknown>:0", "").Replace("HutongGames.", "").Replace("Int32", "Int").Replace("PlayMaker.", "");
			}
		}
	
		public override void Update()
		{
			repeatTimer += Time.deltaTime;
			if (repeatTimer > 1.2f)
			{
				repeatTimer = 0f;
				SlowUpdate();
			}
		}
		
		public void SlowUpdate()
		{
			if (cdPlayerPurchased != null && cdPlayerPurchased.Value)
				LoadCDPlayer();
			if (nosPurchased != null && nosPurchased.Value)
				FixNosForGtPanel();

			gaugeTriggers.gameObject.SetActive(stockPanel.transform.GetChild(7).childCount == 0);

			stockTriggerMeters.SetActive(!GlobalVariables.gtDashInstalled);
			gtPanelTrigger.SetActive(!GlobalVariables.dashInstalled);

			warnings.GetChild(0).gameObject.SetActive(warnings.gameObject.activeSelf);

			gtPanel.transform.GetChild(9).gameObject.SetActive(gtPanel.transform.GetChild(7).childCount == 0);
			stockPanel.transform.GetChild(9).gameObject.SetActive(stockPanel.transform.GetChild(7).childCount == 0);
			gtPanel.transform.GetChild(10).gameObject.SetActive(gtPanel.transform.GetChild(8).childCount == 0);
			stockPanel.transform.GetChild(10).gameObject.SetActive(stockPanel.transform.GetChild(8).childCount == 0);

			bool anyInstalled = GlobalVariables.gtDashInstalled || GlobalVariables.dashInstalled;
			dashInstalledStatus.Value = anyInstalled;
			blinkerDashRequiredBool.Value = anyInstalled;

			if (gtTachometer.transform.position.y < -200)
			{
				isSavingNormally1 = false;
				ResetTach();
			}
			if (gtPanel.transform.position.y < -200)
			{
				isSavingNormally2 = false;
				ResetPanel();
			}
		}

		public override void OnSave()
		{
			isSavingNormally1 = true;
			isSavingNormally2 = true;
			ChangeSaveFile(null, GameObject.Find("Database/DatabaseMechanics/GaugeRPM"), null, true);
			ChangeSaveFile(null, gtTachInfo, null, true);
			ChangeSaveFile(null, GameObject.Find("Database/DatabaseMechanics/GaugeClock"), null, true);
			ChangeSaveFile(null, GameObject.Find("Database/DatabaseMechanics/Radio"), null, true);
			ChangeSaveFile(null, GameObject.Find("Database/DatabaseOrders/CD_player"), null, true);
		}

		public static void ResetMod()
        {
			if (ModLoader.GetCurrentScene().ToString() == "Game" || ModLoader.GetCurrentScene().ToString() == "NewGameIntro")
			{
				isSavingNormally1 = false;
				isSavingNormally2 = false;
				ResetTach();
				ResetPanel();
			}
			else
				resetOnLoad = true;
        }
		public static void CheckIfResset()
		{
			if (resetOnLoad)
			{
				ResetTach();
				ResetPanel();
			}
			resetOnLoad = false;
		}


		


		
	
		public static void LoadTach()
		{
			GameObject saveTach = GameObject.Find("Database/DatabaseMechanics/GaugeRPM");
			gtTachInfo = Object.Instantiate(saveTach);
			gtTachInfo.name = "GT" + saveTach.name;
			gtTachInfo.transform.SetParent(saveTach.transform.parent);



			GameObject tach = saveTach.GetComponent<PlayMakerFSM>().FsmVariables.FindFsmGameObject("ThisPart").Value;
			gtTachometer = Object.Instantiate(tach);
			gtTachometer.name = "GT " + tach.name;
			GlobalVariables.gtTachometer = gtTachometer;



			GameObject triggerTach = saveTach.GetComponent<PlayMakerFSM>().FsmVariables.FindFsmGameObject("Trigger").Value;
			gtTachTrigger = Object.Instantiate(triggerTach);
			gtTachTrigger.name = triggerTach.name + "_GT";
			gtTachTrigger.transform.SetParent(triggerTach.transform.parent);
			gtTachTrigger.transform.localPosition = new Vector3(0, 0, 0);



			FsmHook.FsmInject(gtTachInfo, "Save game", ResetTach);

			gtTachometer.GetComponents<PlayMakerFSM>()[0].FsmVariables.GetFsmGameObject("db_ThisPart").Value = gtTachInfo;
			gtTachometer.GetComponents<PlayMakerFSM>()[1].FsmVariables.GetFsmGameObject("db_ThisPart").Value = gtTachInfo;
			gtTachometer.GetComponents<PlayMakerFSM>()[1].FsmVariables.GetFsmGameObject("db_Trigger").Value = gtTachTrigger;

			gtTachInfo.GetComponents<PlayMakerFSM>()[0].FsmVariables.GetFsmGameObject("ThisPart").Value = gtTachometer;
			gtTachInfo.GetComponents<PlayMakerFSM>()[0].FsmVariables.GetFsmGameObject("Trigger").Value = gtTachTrigger;

			gtTachometer.GetComponents<PlayMakerFSM>()[1].FsmVariables.GetFsmGameObject("Trigger").Value = gtTachTrigger;

			gtTachTrigger.GetComponents<PlayMakerFSM>()[0].FsmVariables.GetFsmGameObject("Part").Value = gtTachometer;
			gtTachTrigger.GetComponents<PlayMakerFSM>()[0].FsmVariables.GetFsmString("HandChild").Value = gtTachometer.name;
			gtTachTrigger.GetComponents<PlayMakerFSM>()[0].FsmVariables.GetFsmGameObject("db_ThisPart").Value = gtTachInfo;
			gtTachTrigger.GetComponents<PlayMakerFSM>()[0].FsmVariables.GetFsmGameObject("Bolts").Value = gtTachometer.transform.GetChild(0).gameObject;

			gtTachometer.transform.GetChild(2).gameObject.GetComponent<Renderer>().materials[0].SetTexture("_MainTex", tachTexture);
			gtTachometer.transform.GetChild(3).GetChild(0).gameObject.GetComponent<Renderer>().materials[0].SetTexture("_MainTex", needleTexture);
			gtTachometer.transform.GetChild(3).GetChild(0).gameObject.GetComponent<Renderer>().materials[0].shader = Shader.Find("Standard");
			gtTachometer.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.TwoSided;

		} // duplicates tach and stuff





		public static void LoadPanel()
		{
			GameObject saveMeters = GameObject.Find("Database/DatabaseMechanics/DashboardMeters");
			gtPanelInfo = Object.Instantiate(saveMeters);
			gtPanelInfo.name = "GT " + saveMeters.name;
			gtPanelInfo.transform.SetParent(saveMeters.transform.parent);

			stockPanel = saveMeters.GetComponent<PlayMakerFSM>().FsmVariables.FindFsmGameObject("ThisPart").Value;
			gtPanel = Object.Instantiate(stockPanel);
			gtPanel.name = "GT " + stockPanel.name;
			GlobalVariables.gtPanel = gtPanel;


			stockTriggerMeters = saveMeters.GetComponent<PlayMakerFSM>().FsmVariables.FindFsmGameObject("Trigger").Value;
			GameObject triggerMeters = stockTriggerMeters;
			gtPanelTrigger = Object.Instantiate(triggerMeters);
			gtPanelTrigger.name = triggerMeters.name + "_GT";
			gtPanelTrigger.transform.SetParent(triggerMeters.transform.parent);
			gtPanelTrigger.transform.localPosition = new Vector3(0, 0, 0);


			gtPanelInfo.GetComponents<PlayMakerFSM>()[0].FsmVariables.GetFsmGameObject("ThisPart").Value = gtPanel;
			gtPanelInfo.GetComponents<PlayMakerFSM>()[0].FsmVariables.GetFsmGameObject("Trigger").Value = gtPanelTrigger;

			gtPanel.GetComponents<PlayMakerFSM>()[1].FsmVariables.GetFsmGameObject("Trigger").Value = gtPanelTrigger;


			gtPanel.GetComponents<PlayMakerFSM>()[0].FsmVariables.GetFsmGameObject("db_ThisPart").Value = gtPanelInfo;
			gtPanel.GetComponents<PlayMakerFSM>()[1].FsmVariables.GetFsmGameObject("db_ThisPart").Value = gtPanelInfo;
			gtPanel.GetComponents<PlayMakerFSM>()[1].FsmVariables.GetFsmGameObject("db_Trigger").Value = gtPanelTrigger;

			gtPanelTrigger.GetComponents<PlayMakerFSM>()[0].FsmVariables.GetFsmGameObject("Part").Value = gtPanel;
			gtPanelTrigger.GetComponents<PlayMakerFSM>()[0].FsmVariables.GetFsmString("HandChild").Value = gtPanel.name;
			gtPanelTrigger.GetComponents<PlayMakerFSM>()[0].FsmVariables.GetFsmGameObject("db_ThisPart").Value = gtPanelInfo;
			gtPanelTrigger.GetComponents<PlayMakerFSM>()[0].FsmVariables.GetFsmGameObject("Bolts").Value = gtPanel.transform.GetChild(2).gameObject;

			FsmHook.FsmInject(gtPanelInfo, "Save game", ResetPanel);



			gtPanel.transform.GetChild(4).gameObject.GetComponent<PlayMakerFSM>().FsmVariables.GetFsmGameObject("db_DashboardMeters").Value = gtPanelInfo; // fixed knobs
		}// duplicates panel and stuff
		
		public static void DeleteDuplicatedOnGtPanel()
		{
			Transform gaugePivot = gtPanel.transform.GetChild(7);
			gaugeTriggers = gtPanel.transform.GetChild(9);
			radioTriggers = gtPanel.transform.GetChild(10);
			Transform radioPivot = gtPanel.transform.GetChild(8);
			
			warnings = gtPanel.transform.GetChild(5);
			
			for (int i = 0; i < warnings.childCount - 1; i++)
				Object.Destroy(warnings.GetChild(i).gameObject);

			for (int i = 0; i < radioPivot.childCount; i++)
				Object.Destroy(radioPivot.GetChild(i).gameObject);

			for (int i = 0; i < gaugePivot.childCount; i++)
				Object.Destroy(gaugePivot.GetChild(i).gameObject);


			for (int i = 0; i < radioTriggers.childCount; i++)
			{
				radioTriggers.GetChild(i).gameObject.GetComponent<PlayMakerFSM>().FsmVariables.GetFsmGameObject("Parent").Value = radioPivot.gameObject;
				radioTriggers.GetChild(i).gameObject.GetComponent<PlayMakerFSM>().FsmVariables.GetFsmGameObject("Triggers").Value = radioTriggers.gameObject;
			}
			for (int i = 0; i < gaugeTriggers.childCount; i++)
			{
				if (gaugeTriggers.GetChild(i).gameObject.name.Contains("ECORPM"))
					return;
				gaugeTriggers.GetChild(i).gameObject.GetComponent<PlayMakerFSM>().FsmVariables.GetFsmGameObject("Parent").Value = gaugePivot.gameObject;
				gaugeTriggers.GetChild(i).gameObject.GetComponent<PlayMakerFSM>().FsmVariables.GetFsmGameObject("Triggers").Value = gaugeTriggers.gameObject;
			}
			
			gaugeTriggers.gameObject.SetActive(true);
			radioTriggers.gameObject.SetActive(true);
		} // deletes objects that were instantiated together with panel, so you don't have 2 radios, gauges, etc.
		public static void FixPartsThatGetInstalledOnGtPanel()
		{
			DeleteDuplicatedOnGtPanel();


			GameObject rpmSave = GameObject.Find("Database/DatabaseMechanics/GaugeRPM");
			GameObject clockSave = GameObject.Find("Database/DatabaseMechanics/GaugeClock");
			GameObject radioSave = GameObject.Find("Database/DatabaseMechanics/Radio");
			cdSave = GameObject.Find("Database/DatabaseOrders/CD_player");
			nosSave = GameObject.Find("Database/DatabaseOrders/N2O Button Panel");


			GameObject rpmGauge = rpmSave.GetComponent<PlayMakerFSM>().FsmVariables.FindFsmGameObject("ThisPart").Value;
			GameObject clockGauge = clockSave.GetComponent<PlayMakerFSM>().FsmVariables.FindFsmGameObject("ThisPart").Value;
			GameObject radio = radioSave.GetComponent<PlayMakerFSM>().FsmVariables.FindFsmGameObject("ThisPart").Value;
			cd = cdSave.GetComponent<PlayMakerFSM>().FsmVariables.FindFsmGameObject("ThisPart").Value;


			GameObject rpmTrigger = gaugeTriggers.GetChild(1).gameObject;
			GameObject clockTrigger = gaugeTriggers.GetChild(0).gameObject;
			GameObject gtTachGTTrigger = gaugeTriggers.GetChild(2).gameObject;
			GameObject radioTrigger = radioTriggers.GetChild(1).gameObject;
			cdTrigger = radioTriggers.GetChild(0).gameObject;

			nosTrigger = gtPanel.transform.GetChild(11).gameObject;
			nosStockTrigger = stockPanel.transform.GetChild(11).gameObject;
			nosObject = gtPanel.transform.GetChild(6).gameObject;
			nosTrigger.GetComponent<PlayMakerFSM>().FsmVariables.GetFsmGameObject("ActivateThis").Value = nosObject;
			nosTrigger.gameObject.SetActive(true);

			

			ChangeSaveFile(rpmGauge, rpmSave, rpmTrigger, false);
			ChangeSaveFile(clockGauge, clockSave, clockTrigger, false);
			ChangeSaveFile(gtTachometer, gtTachInfo, gtTachGTTrigger, false);
			ChangeSaveFile(radio, radioSave, radioTrigger, false);

			


			rpmSave.GetComponent<PlayMakerFSM>().SendEvent("EXISTS");
			clockSave.GetComponent<PlayMakerFSM>().SendEvent("EXISTS");
			gtTachInfo.GetComponent<PlayMakerFSM>().SendEvent("EXISTS"); // temp load
			radioSave.GetComponent<PlayMakerFSM>().SendEvent("EXISTS");
			

			ChangeSaveFile(null, rpmSave, null, true);
			ChangeSaveFile(null, clockSave, null, true);
			ChangeSaveFile(null, gtTachInfo, null, true);
			ChangeSaveFile(null, radioSave, null, true);


			ChangeObjectFile(rpmGauge);
			ChangeObjectFile(clockGauge);
			ChangeObjectFile(gtTachometer);
			ChangeObjectFile(radio);


			ChangeTriggerFile(rpmTrigger);
			ChangeTriggerFile(clockTrigger);
			ChangeTriggerFile(gtTachGTTrigger);
			ChangeTriggerFile(radioTrigger);

			rpmSave.GetComponent<PlayMakerFSM>().SendEvent("EXISTS");
			clockSave.GetComponent<PlayMakerFSM>().SendEvent("EXISTS");
			gtTachInfo.GetComponent<PlayMakerFSM>().SendEvent("EXISTS"); // final load
			radioSave.GetComponent<PlayMakerFSM>().SendEvent("EXISTS");



			PlayMakerFSM radioStatusFsm = GameObject.Find("Database/PartsStatus/Radio").GetComponent<PlayMakerFSM>();

			dashInstalledStatus = radioStatusFsm.FsmVariables.FindFsmBool("Required3");
			FsmState cdCheck = radioStatusFsm.FsmStates.First((FsmState state) => state.Name == "Check CD");
			FsmState radioCheck = radioStatusFsm.FsmStates.First((FsmState state) => state.Name == "Check Radio");
			cdCheck.Actions[2] = emptyAction;
			radioCheck.Actions[2] = emptyAction;

			gtPanel.transform.GetChild(5).gameObject.SetActive(true); // enables warning light object


			cdPlayerPurchased = cdSave.GetComponent<PlayMakerFSM>().FsmVariables.FindFsmBool("Purchased");
			nosPurchased = nosSave.GetComponent<PlayMakerFSM>().FsmVariables.FindFsmBool("Purchased");

			if (cdPlayerPurchased.Value)
				LoadCDPlayer();
            if (nosPurchased.Value)
                FixNosForGtPanel();
        } // fixed all the refrences to panel
		public static void LoadCDPlayer()
		{
			cdPlayerPurchased = null;
			

			ChangeObjectFile(cd);
			ChangeTriggerFile(cdTrigger);
			ChangeSaveFileCD(cd, cdSave, cdTrigger, false);

		}

		

		public static void ChangeSomeGauges()
		{
				satsuma.AddComponent<Status>();
				satsuma.AddComponent<LightingManager>();
		}
		public static void RefrenceStuff()
		{
			GameObject dashButtons = gtPanel.transform.GetChild(4).GetChild(0).gameObject;
			bool dashButtonsActive = dashButtons.activeSelf;
			dashButtons.SetActive(true);
			PlayMakerFSM lightFsm = dashButtons.transform.GetChild(1).gameObject.GetComponent<PlayMakerFSM>();
			FsmState offState = lightFsm.FsmStates.First((FsmState state) => state.Name == "Off");
			offState.Actions[0] = emptyAction;
			dashButtons.SetActive(dashButtonsActive);
		}





		public static void Blinkers(bool leftOn, bool rightOn)
		{
			warnings.GetChild(0).GetChild(1).gameObject.SetActive(leftOn);
			warnings.GetChild(0).GetChild(0).gameObject.SetActive(rightOn);
		}
		public void FixBlinkersForGtPanel()
		{
			PlayMakerFSM blinkerStatusFsm = satsuma.transform.FindChild("Dashboard").GetChild(7).gameObject.GetComponents<PlayMakerFSM>()[1];


			FsmState checkParts = blinkerStatusFsm.FsmStates.First((FsmState state) => state.Name == "Check parts");

			FsmState state2 = blinkerStatusFsm.FsmStates.First((FsmState state) => state.Name == "State 2");

			FsmState hazardOn = blinkerStatusFsm.FsmStates.First((FsmState state) => state.Name == "Hazard ON");
			FsmState off = blinkerStatusFsm.FsmStates.First((FsmState state) => state.Name == "Off");

			FsmState leftOn = blinkerStatusFsm.FsmStates.First((FsmState state) => state.Name == "Left ON");
			FsmState off2 = blinkerStatusFsm.FsmStates.First((FsmState state) => state.Name == "Off 2");

			FsmState rightOn = blinkerStatusFsm.FsmStates.First((FsmState state) => state.Name == "Right ON");
			FsmState off3 = blinkerStatusFsm.FsmStates.First((FsmState state) => state.Name == "Off 3");


			MethodInfo a = GetType().GetMethod("Blinkers");
			object[] turnBothOn = new object[]
			{
				true,
				true
			};
			object[] turnLeftOn = new object[]
			{
				true,
				false
			};
			object[] turnRightOn = new object[]
			{
				false,
				true
			};
			object[] turnBothOff = new object[]
			{
				false,
				false
			};

			hazardOn.FsmVoidHook(a, turnBothOn, 1);
			off.FsmVoidHook(a, turnBothOff, 1);
			leftOn.FsmVoidHook(a, turnLeftOn, 1);
			off2.FsmVoidHook(a, turnBothOff, 1);
			rightOn.FsmVoidHook(a, turnRightOn, 1);
			off3.FsmVoidHook(a, turnBothOff, 1);
			state2.FsmVoidHook(a, turnBothOff, 1);

			checkParts.Actions[1] = emptyAction;


			blinkerDashRequiredBool = blinkerStatusFsm.FsmVariables.FindFsmBool("Required2");
		}
		



		public static void UpdateTalos()
		{
			selection.Value = GlobalVariables.gtDashInstalled ? selectionsFromGt.Value : selectionsFromStock.Value;
		}
		public static void TaalasGaismas()
		{
			selectionsFromGt = gtPanel.transform.GetChild(4).GetChild(0).GetChild(1).gameObject.GetComponent<PlayMakerFSM>().FsmVariables.FindFsmInt("Selection");
			selectionsFromStock = stockPanel.transform.GetChild(4).GetChild(0).GetChild(1).gameObject.GetComponent<PlayMakerFSM>().FsmVariables.FindFsmInt("Selection");
			GameObject talie = satsuma.transform.GetChild(12).GetChild(5).GetChild(1).GetChild(4).gameObject;
			PlayMakerFSM taloFsms = talie.GetComponent<PlayMakerFSM>();
			selection = taloFsms.FsmVariables.FindFsmInt("Selection");
			FsmState fsmstate = taloFsms.FsmStates.First((FsmState state) => state.Name == "Test");
			fsmstate.Actions[0] = emptyAction;
			FsmHook.FsmInject(talie, "Test", UpdateTalos);
		}




		public static void ChangeSaveFile(GameObject item, GameObject saveitem, GameObject secondTrig, bool justUpdate)
		{
			PlayMakerFSM fsm = saveitem.GetComponent<PlayMakerFSM>();
			FsmVariables variables = fsm.FsmVariables;
			FsmState assembleState = fsm.FsmStates.First((FsmState state) => state.Name == "Assemble");
			FsmBool regularInstalledBool = variables.FindFsmBool("Installed");

			if (!justUpdate)
			{
				FsmState loadState = fsm.FsmStates.First((FsmState state) => state.Name == "Load game");
				FsmState saveState = fsm.FsmStates.First((FsmState state) => state.Name == "Save game");
				FsmState checkDataState = fsm.FsmStates.First((FsmState state) => state.Name == "Check data");
				FsmState basicState = fsm.FsmStates.First((FsmState state) => state.Name == "Basic state");


				FsmBool gtInstalledBool = new FsmBool
				{
					Name = "InstalledGT",
					Value = false
				};
				variables.BoolVariables = variables.BoolVariables.Insert(gtInstalledBool);


				FsmString gtUniqueTag = new FsmString
				{
					Name = "UniqueTagInstalledGt",
					Value = item.name + "InstalledGt"
				};
				variables.StringVariables = variables.StringVariables.Insert(gtUniqueTag);


				FsmGameObject gtTriggerObject = new FsmGameObject
				{
					Name = "TriggerGt",
					Value = secondTrig
				};
				variables.GameObjectVariables = variables.GameObjectVariables.Insert(gtTriggerObject);






				LoadBool loadGtInstalledBool = new LoadBool
				{
					uniqueTag = gtUniqueTag,
					saveFile = "defaultES2File.txt",
					loadFromResources = false,
					loadValue = gtInstalledBool
				};
				loadState.Actions = loadState.Actions.Insert(loadGtInstalledBool, loadState.Actions.Length - 2);



				SaveBool SaveGtInstalledBool = new SaveBool
				{
					saveValue = gtInstalledBool,
					uniqueTag = gtUniqueTag,
					saveFile = "defaultES2File.txt"
				};
				saveState.Actions = saveState.Actions.Insert(SaveGtInstalledBool);






				BuildString buildGtInstalledString = new BuildString
				{
					storeResult = gtUniqueTag,
					separator = (checkDataState.Actions[1] as BuildString).separator,
					stringParts = (checkDataState.Actions[1] as BuildString).stringParts
				};
				checkDataState.Actions = checkDataState.Actions.Insert(buildGtInstalledString);




				Exists modInfoExists = new Exists
				{
					uniqueTag = gtUniqueTag,
					ifExists = fsm.FsmEvents[1],
					ifDoesNotExist = fsm.FsmEvents[3],
					saveFile = "defaultES2File.txt"
				};
				checkDataState.Actions = checkDataState.Actions.Insert(modInfoExists);



				(assembleState.Actions[0] as SetFsmGameObject).gameObject.GameObject = null;
				(assembleState.Actions[1] as SetFsmBool).gameObject.GameObject = null;
			}
			else
			{
				FsmBool gtInstalledBool = variables.FindFsmBool("InstalledGT");
				FsmGameObject gtTriggerObject = variables.FindFsmGameObject("TriggerGt");

				if (gtInstalledBool.Value)
				{
					(assembleState.Actions[0] as SetFsmGameObject).gameObject.GameObject = gtTriggerObject;
					(assembleState.Actions[1] as SetFsmBool).gameObject.GameObject = gtTriggerObject;
				}
				else
				{
					(assembleState.Actions[0] as SetFsmGameObject).gameObject.GameObject = variables.GetFsmGameObject("Trigger");
					(assembleState.Actions[1] as SetFsmBool).gameObject.GameObject = variables.GetFsmGameObject("Trigger");
				}
			}

		}
		public static void ChangeSaveFileCD(GameObject item, GameObject saveitem, GameObject secondTrig, bool justUpdate)
		{
			PlayMakerFSM fsm = saveitem.GetComponent<PlayMakerFSM>();
			FsmVariables variables = fsm.FsmVariables;
			FsmState assembleState = fsm.FsmStates.First((FsmState state) => state.Name == "Assemble");
			FsmBool regularInstalledBool = variables.FindFsmBool("Installed");

			if (!justUpdate)
			{
				FsmState loadState = fsm.FsmStates.First((FsmState state) => state.Name == "Load game");
				FsmState saveState = fsm.FsmStates.First((FsmState state) => state.Name == "Save game");
				FsmState checkDataState = fsm.FsmStates.First((FsmState state) => state.Name == "Check data");
				FsmState basicState = fsm.FsmStates.First((FsmState state) => state.Name == "Basic state");


				FsmBool gtInstalledBool = new FsmBool
				{
					Name = "InstalledGT",
					Value = false
				};
				variables.BoolVariables = variables.BoolVariables.Insert(gtInstalledBool);


				FsmString gtUniqueTag = new FsmString
				{
					Name = "UniqueTagInstalledGt",
					Value = item.name + "InstalledGt"
				};
				variables.StringVariables = variables.StringVariables.Insert(gtUniqueTag);


				FsmGameObject gtTriggerObject = new FsmGameObject
				{
					Name = "TriggerGt",
					Value = secondTrig
				};
				variables.GameObjectVariables = variables.GameObjectVariables.Insert(gtTriggerObject);






				LoadBool loadGtInstalledBool = new LoadBool
				{
					uniqueTag = gtUniqueTag,
					saveFile = "defaultES2File.txt",
					loadFromResources = false,
					loadValue = gtInstalledBool
				};
				loadState.Actions = loadState.Actions.Insert(loadGtInstalledBool, 3);



				SaveBool SaveGtInstalledBool = new SaveBool
				{
					saveValue = gtInstalledBool,
					uniqueTag = gtUniqueTag,
					saveFile = "defaultES2File.txt"
				};
				saveState.Actions = saveState.Actions.Insert(SaveGtInstalledBool);

				FsmEvent assembleGtEvent = new FsmEvent("INSTALLEDGT");
				fsm.Fsm.Events = fsm.Fsm.Events.Insert(assembleGtEvent);


                FsmState assembleGT = new FsmState(fsm.Fsm)
                {
                    Name = "AssembleGT",
                    Actions = new FsmStateAction[]
                    {
                        new SetFsmBool
                        {
                            gameObject = new FsmOwnerDefault{GameObject = gtTriggerObject },
                            fsmName = "Assembly",
                            variableName = "Setup",
                            setValue = true,
                            everyFrame = false
                        }
                    },
                    Transitions = new FsmTransition[]
                    {
                        new FsmTransition
                        {
                            FsmEvent = fsm.FsmEvents[0],
                            ToState = "Activate"
                        }
                    }
                };
				FsmTransition gtAssemble = new FsmTransition
				{
					FsmEvent = assembleGtEvent,
					ToState = "AssembleGT"
				};
				fsm.Fsm.States = fsm.Fsm.States.Insert(assembleGT);

				basicState.Transitions = basicState.Transitions.Insert(gtAssemble);

				basicState.Actions = basicState.Actions.Insert(new BoolAllTrue
				{
					boolVariables = new FsmBool[] { gtInstalledBool, regularInstalledBool },
					sendEvent = assembleGtEvent
				}, 2);

			

				BuildString buildGtInstalledString = new BuildString
				{
					storeResult = gtUniqueTag,
					separator = (checkDataState.Actions[1] as BuildString).separator,
					stringParts = (checkDataState.Actions[1] as BuildString).stringParts
				};
				checkDataState.Actions = checkDataState.Actions.Insert(buildGtInstalledString);




				Exists modInfoExists = new Exists
				{
					uniqueTag = gtUniqueTag,
					ifExists = fsm.FsmEvents[1],
					ifDoesNotExist = fsm.FsmEvents[3],
					saveFile = "defaultES2File.txt"
				};
				checkDataState.Actions = checkDataState.Actions.Insert(modInfoExists);



				(assembleState.Actions[0] as SetFsmGameObject).gameObject.GameObject = null;
				(assembleState.Actions[1] as SetFsmBool).gameObject.GameObject = null;

				//FsmEvent reloadEvent = new FsmEvent("RELOAD");
				//fsm.Fsm.Events = fsm.Fsm.Events.Insert(reloadEvent);
				//FsmTransition reload = new FsmTransition
				//{
				//	FsmEvent = reloadEvent,
				//	ToState = "Load game"
				//};

				//fsm.FsmStates.First((FsmState state) => state.Name == "Activate").Transitions = fsm.FsmStates.First((FsmState state) => state.Name == "Activate").Transitions.Insert(reload);

				//item.SetActive(false);
				//fsm.SendEvent("RELOAD");

			}
			else
			{
				FsmBool gtInstalledBool = variables.FindFsmBool("InstalledGT");
				FsmGameObject gtTriggerObject = variables.FindFsmGameObject("TriggerGt");

				if (gtInstalledBool.Value)
				{
					(assembleState.Actions[0] as SetFsmGameObject).gameObject.GameObject = gtTriggerObject;
					(assembleState.Actions[1] as SetFsmBool).gameObject.GameObject = gtTriggerObject;
				}
				else
				{
					(assembleState.Actions[0] as SetFsmGameObject).gameObject.GameObject = variables.GetFsmGameObject("Trigger");
					(assembleState.Actions[1] as SetFsmBool).gameObject.GameObject = variables.GetFsmGameObject("Trigger");
				}
			}

		}
		public static void ChangeTriggerFile(GameObject trigger)
		{
			if (trigger == null)
				throw new NullReferenceException("trigger was null in " + MethodBase.GetCurrentMethod().Name);
			PlayMakerFSM removal = trigger.GetComponent<PlayMakerFSM>();
			FsmState removeState = removal.FsmStates.First((FsmState state) => state.Name == "Assemble 2");

			SetFsmBool setGttoTrue = new SetFsmBool
			{
				fsmName = "Data",
				variableName = "InstalledGT",
				setValue = true,
				gameObject = (removeState.Actions[6] as SetFsmBool).gameObject
			};

			removeState.Actions = removeState.Actions.Insert(setGttoTrue);
		}
		public static void ChangeObjectFile(GameObject item)
		{
			if (item == null)
				throw new NullReferenceException("item was null " + MethodBase.GetCurrentMethod().Name);

			PlayMakerFSM[] components = item.GetComponents<PlayMakerFSM>();
			PlayMakerFSM removal = components[components.Length - 1];
			if (removal == null)
				throw new NullReferenceException("no fsm found " + MethodBase.GetCurrentMethod().Name + " item: " + item.name);

			


			FsmState removeState = removal.FsmStates.First((FsmState state) => state.Name == "Remove part");
			if (removeState == null)
				throw new NullReferenceException("no Remove part state found " + MethodBase.GetCurrentMethod().Name + " item: " + item.name);
			SetFsmBool setGttoFalse = new SetFsmBool
			{
				fsmName = "Data",
				variableName = "InstalledGT",
				setValue = false,
				gameObject = new FsmOwnerDefault
				{
					GameObject = removal.FsmVariables.FindFsmGameObject("db_ThisPart").Value
				}
			};

			removeState.Actions = removeState.Actions.Insert(setGttoFalse);
		}



		public static void ChangeBrightness()
		{
			MasterAudio.PlaySound3DAndForget("CarFoley", lightPotentiometer.transform, false, 0.2f, null, 0f, "gear_grind5");
			float potRot = (lightPotentiometerFsm.FsmVariables.FindFsmFloat("KnobPos").Value + 250) / 2.8f;
			brightnessPotResistane = Mathf.Log(potRot / 20 + 1, 5) * potValue + 0.8f;
		}
		public static void AddBrightnessKnob()
		{
			lightPotentiometer = Object.Instantiate(gtPanel.transform.GetChild(4).GetChild(3).gameObject);
			lightPotentiometer.transform.parent = gtPanel.transform.GetChild(4);
			lightPotentiometer.transform.localPosition = new Vector3(0.102f, 0.602f, 0.88f);
			lightPotentiometer.transform.localEulerAngles = new Vector3(-10, 0, 0);
			lightPotentiometer.name = "KnobDashBrightness";
			lightPotentiometer.transform.GetChild(0).localEulerAngles = new Vector3(0, -250, 0);

			lightPotentiometerFsmObject = Object.Instantiate(gtPanel.transform.GetChild(4).GetChild(0).GetChild(3).gameObject);
			lightPotentiometerFsmObject.transform.parent = gtPanel.transform.GetChild(4).GetChild(0);
			lightPotentiometerFsmObject.transform.localPosition = new Vector3(0.102f, 0.602f, 0.88f);
			lightPotentiometerFsmObject.transform.localEulerAngles = new Vector3(-10, 0, 0);
			lightPotentiometerFsmObject.name = "DashBrightness";
			Object.Destroy(lightPotentiometerFsmObject.GetComponents<PlayMakerFSM>()[1]);

			lightPotentiometerFsm = lightPotentiometerFsmObject.GetComponent<PlayMakerFSM>();
			lightPotentiometerFsm.FsmVariables.FindFsmGameObject("Target").Value = lightPotentiometer.transform.GetChild(0).gameObject;
			FsmState fsmstate = lightPotentiometerFsm.FsmStates.First((FsmState state) => state.Name == "Wait button");

			lightPotentiometerFsm.FsmVariables.FindFsmFloat("KnobPos").Value = -250f;




			FsmState increase = lightPotentiometerFsm.FsmStates.First((FsmState state) => state.Name == "INCREASE");
			increase.Actions[2] = emptyAction;
			increase.Actions[3] = emptyAction;
			increase.Actions[4] = emptyAction;

			FsmState decrease = lightPotentiometerFsm.FsmStates.First((FsmState state) => state.Name == "DECREASE");
			decrease.Actions[2] = emptyAction;
			decrease.Actions[3] = emptyAction;
			decrease.Actions[4] = emptyAction;


			(fsmstate.Actions[0] as SetStringValue).stringValue = "BRIGHTNESS";

			(increase.Actions[0] as FloatAdd).add = -10;
			(increase.Actions[1] as FloatClamp).minValue = -250;
			(increase.Actions[1] as FloatClamp).maxValue = 20;
			(decrease.Actions[0] as FloatAdd).add = 10;
			(decrease.Actions[1] as FloatClamp).minValue = -250;
			(decrease.Actions[1] as FloatClamp).maxValue = 20;

			lightPotentiometerFsm.FsmVariables.FindFsmFloat("Choke").Name = "Scroll";

			GetAxis scrollCheck = new GetAxis
			{
				axisName = "Mouse ScrollWheel",
				multiplier = 1,
				store = lightPotentiometerFsm.FsmVariables.FindFsmFloat("Scroll"),
				everyFrame = true
			};
			SetRotation setRot = new SetRotation
			{
				xAngle = 0,
				yAngle = lightPotentiometerFsm.FsmVariables.FindFsmFloat("KnobPos"),
				zAngle = 0,
				everyFrame = true,
				vector = new Vector3(0, 0, 0),
				quaternion = new Quaternion(0, 0, 0, 0)
			};
			setRot.space = (increase.Actions[5] as SetPosition).space;
			setRot.gameObject = (increase.Actions[5] as SetPosition).gameObject;


			FloatCompare comparePos = new FloatCompare
			{
				float1 = lightPotentiometerFsm.FsmVariables.FindFsmFloat("Scroll"),
				float2 = 0,
				tolerance = 0,
				lessThan = lightPotentiometerFsm.FsmEvents[1],
				greaterThan = lightPotentiometerFsm.FsmEvents[2],
				everyFrame = true
			};
			FloatCompare comparePosI = new FloatCompare
			{
				float1 = lightPotentiometerFsm.FsmVariables.FindFsmFloat("Scroll"),
				float2 = 0,
				tolerance = 0,
				lessThan = lightPotentiometerFsm.FsmEvents[1],
				equal = lightPotentiometerFsm.FsmEvents[4],
				everyFrame = true
			};
			FloatCompare comparePosD = new FloatCompare
			{
				float1 = lightPotentiometerFsm.FsmVariables.FindFsmFloat("Scroll"),
				float2 = 0,
				tolerance = 0,
				equal = lightPotentiometerFsm.FsmEvents[4],
				greaterThan = lightPotentiometerFsm.FsmEvents[2],
				everyFrame = true
			};
			decrease.Actions[7] = comparePosD;
			decrease.Actions[5] = setRot;
			increase.Actions[5] = setRot;
			increase.Actions[7] = comparePosI;
			increase.Actions[8] = scrollCheck;
			decrease.Actions[8] = scrollCheck;
			fsmstate.Actions[3] = scrollCheck;
			fsmstate.Actions[4] = comparePos;

			lightPotentiometer.transform.GetChild(0).gameObject.GetComponent<Renderer>().materials[0].SetTexture("_MainTex", switchTexture);

			FsmHook.FsmInject(lightPotentiometerFsmObject, "INCREASE", ChangeBrightness);
			FsmHook.FsmInject(lightPotentiometerFsmObject, "DECREASE", ChangeBrightness);

			ChangeBrightness();
		}




		public static void ChangeDashTextures()
		{
			gtPanel.transform.FindChild("GaugesMesh").GetComponent<Renderer>().materials[0].SetTexture("_MainTex", dashTexture);
			gtPanel.transform.FindChild("GaugesMesh").GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.TwoSided;
			gtPanel.transform.FindChild("GaugesMesh").GetComponent<Renderer>().receiveShadows = true;
			gtPanel.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.TwoSided;
			gtPanel.GetComponent<Renderer>().receiveShadows = true;



			gtPanel.transform.GetChild(3).GetChild(0).GetChild(0).gameObject.GetComponent<Renderer>().materials[0].shader = Shader.Find("Standard");
			gtPanel.transform.GetChild(3).GetChild(1).GetChild(0).gameObject.GetComponent<Renderer>().materials[0].shader = Shader.Find("Standard");
			gtPanel.transform.GetChild(3).GetChild(3).GetChild(0).gameObject.GetComponent<Renderer>().materials[0].shader = Shader.Find("Standard");
			gtPanel.transform.GetChild(3).GetChild(0).GetChild(0).gameObject.GetComponent<Renderer>().materials[0].SetTexture("_MainTex", needleTexture);
			gtPanel.transform.GetChild(3).GetChild(1).GetChild(0).gameObject.GetComponent<Renderer>().materials[0].SetTexture("_MainTex", needleTexture);
			gtPanel.transform.GetChild(3).GetChild(3).GetChild(0).gameObject.GetComponent<Renderer>().materials[0].SetTexture("_MainTex", needleTexture);


			gtPanel.transform.GetChild(3).GetChild(2).GetChild(0).gameObject.GetComponent<Renderer>().materials[0].SetTexture("_MainTex", odometerRedText);
			for (int i = 1; i < gtPanel.transform.GetChild(3).GetChild(2).childCount; i++)
			{
				gtPanel.transform.GetChild(3).GetChild(2).GetChild(i).gameObject.GetComponent<Renderer>().materials[0].SetTexture("_MainTex", odometerText);
			}
			if (whiteDash)
			{
				gtPanel.GetComponent<Renderer>().materials[0].SetTexture("_MainTex", dashboardTexture);
				gtPanel.GetComponent<Renderer>().materials[1].SetTexture("_MainTex", dashboardSurface);
			}
			else
			{
				gtPanel.GetComponent<Renderer>().materials[0].SetTexture("_MainTex", switchTexture);
			}
		}
		public void LoadTextures()
		{
			dashTexture = LoadAssets.LoadTexture(this, "satsuma_dash_gauges.png");
			tachTexture = LoadAssets.LoadTexture(this, "rpm_gauge.tex.dds");
			needleTexture = LoadAssets.LoadTexture(this, "gaugeNeedles.png");
			odometerText = LoadAssets.LoadTexture(this, "odometerDigits.png");
			odometerRedText = LoadAssets.LoadTexture(this, "odometerDigitsRed.png");


			dashboardTexture = LoadAssets.LoadTexture(this, "dashboard.png");
			dashboardSurface = LoadAssets.LoadTexture(this, "dashboard_surface.png");
			originalDashboardSurface = LoadAssets.LoadTexture(this, "dashboard_surface_original.png");

			switchTexture = LoadAssets.LoadTexture(this, "switch.png");
		}
		public static void UpdateDashTexture()
		{
			whiteDash = bool.Parse(dashChoice.GetValue().ToString());
			if (whiteDash)
			{
				gtPanel.GetComponent<Renderer>().materials[0].SetTexture("_MainTex", dashboardTexture);
				gtPanel.GetComponent<Renderer>().materials[1].SetTexture("_MainTex", dashboardSurface);
			}
			else
			{
				gtPanel.GetComponent<Renderer>().materials[0].SetTexture("_MainTex", switchTexture);
				gtPanel.GetComponent<Renderer>().materials[1].SetTexture("_MainTex", originalDashboardSurface);
			}
		}






		



		public static void ResetTach()
		{
			if (!isSavingNormally1)
			{
				isSavingNormally1 = true;

				gtTachometer.GetComponents<PlayMakerFSM>()[1].SendEvent("REMOVE");
				gtTachometer.transform.localPosition = new Vector3(23.8f, -0.51f, -52.06f);
				gtTachometer.transform.localEulerAngles = new Vector3(274f, 216f, 200f);
				gtTachInfo.GetComponent<PlayMakerFSM>().SendEvent("SAVEGAME");
				modLog += "\n <color=yellow> GT Tach Reset! </color>";
			}
		}
		public static void ResetPanel()
		{
			if (!isSavingNormally2)
			{
				isSavingNormally2 = true;

				gtPanel.GetComponents<PlayMakerFSM>()[1].SendEvent("REMOVE");
				gtPanel.transform.localPosition = new Vector3(-1261.08f, 0.72f, -603.96f);
				gtPanel.transform.localEulerAngles = new Vector3(3.15f, 8.65f, 104.26f);
				gtPanelInfo.GetComponent<PlayMakerFSM>().SendEvent("SAVEGAME");
				modLog += "\n <color=yellow> GT Panel Reset! </color>";
			}
		}





		
	
		public static string DividerString()
		{
			StringBuilder resultBuilder = new StringBuilder(45);
			for (int i = 0; i < 45; i++)
			{
				resultBuilder.Insert(i, '=');
			}
			return "\n" + resultBuilder.ToString();
		}
	




		


		
		public static void FixNosForGtPanel()
		{
			nosPurchased = null;


			PlayMakerFSM nosFsm = nosObject.transform.GetChild(1).gameObject.GetComponent<PlayMakerFSM>();
			nosFsm.FsmVariables.FindFsmGameObject("db_Required3").Value = gtPanelInfo;
		}
		
	}
}
