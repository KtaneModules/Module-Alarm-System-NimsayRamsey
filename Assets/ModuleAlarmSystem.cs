using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using KModkit;
using RNG = UnityEngine.Random;

public class ModuleAlarmSystem : MonoBehaviour {

	public KMBombInfo Bomb;
	public KMAudio Audio;
	public KMNeedyModule Needy;
	public KMBossModule BossInfo;

	public Material[] LightMat; //UnlitGreen LitGreen UnlitOrange LitOrange
	public Renderer[] LEDs;
	public KMSelectable[] Keypad;
	public KMSelectable ModeButton;
	public TextMesh SegmentDisplay; //Default is 
	//public int[] displayValues;

	//-----------------------------------------------------//
	//READONLY LIBRARIES
	private int MODE = 0;
	private int SubmitIndex = 0;
	private int[] SubmitCode = {0, 0, 0, 0, 0};
	private int[] SolveCode = {0, 0, 0, 0, 0};
	private int SolveMODE = 0;
	private int[,] LetterCodes = {
		{0, 1, 0, 0, 1},
		{1, 0, 1, 0, 1},
		{1, 1, 0, 1, 0},
		{0, 0, 0, 1, 0},
		{1, 1, 1, 1, 1}
	};

	private int TripCount = 0;
	private int solveCount = 0;
	private string Tletter;
	private int Tnumber;

	private bool TRIPPED = false;
	private bool Tflag = false;

	private int SOLVES;
	private string MostRecent;
    private List<string> SolveList = new List<string> { };

	//                                              0    1    2    3    4    5    6    7    8    9    0    1    2    3    4    5    6    7    8    9    0    1    2    3    4    5
	private string[] AlphabetLET = new string[26] {"A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z"};
	private int[] AlphabetVAL = new int[26] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5};
	private int[,] CODEletterPick = new int[,] {
		{0, 10, 20},// 0
		{1, 11, 21},// 1
		{2, 12, 22},// 2
		{3, 13, 23},// 3
		{4, 4, 24},//  4
		{5, 15, 25},// 5
		{6, 16, 6},//  6
		{7, 17, 17},// 7
		{8, 8, 8},//   8
		{9, 19, 9}//   9
	};
	private int[] invertList = {9, 8, 7, 6, 5, 4, 3, 2, 1, 0};
	private List<string> BOSSLIST = new List<string> {
		//"The Boss Example Module",//Example Module

		//Safes
		"Safety Safe",
		"Combination Lock",
		"The Jewel Vault",
		//Keypads
		"Number Pad",
		"Not Number Pad",
		"Number Sequence",
		"Burglar Alarm",
		"Passcodes",
		"Prime Encryption",
		//Interfaces
		"The Generator",
		"Double-Oh",
		"Cursed Double-Oh",
		"Not Double-Oh",
		"Factory Code",
		"Sysadmin",
		"Web Design",
		"Scripting",
		"Waste Management",
		//Military Instruments
		"Silo Authorization",
		"Military Encryption",
		"Battleship",
		"Encrypted Morse",
		"Morsematics",
		"Not Morsematics",
		//REDACTED
		"The Crystal Maze",
		"The Cube",
		"Lightspeed",
		"V",
		//Others
		"The Stock Market",
		"Crypto Market",
		"Algorithmia",
		"Silly Slots"
	};
	private List<string> BLACKLIST = new List<string> {
		"Doomsday Button",
		"Castor",
		"Pollux",
		"X",
		"Y",
		"Turn The Key",
		"Turn The Keys",
		"Custom Keys"
	};

	private bool needyActive = false;
	private int statusLAG = 0;
	private int lagTime = 180;
	//-----------------------------------------------------//
	
	int moduleId;
	static int moduleIdCounter = 1;

	void Awake() {
		moduleId = moduleIdCounter++;
		//TRIPPED = true;
		GetComponent<KMNeedyModule>().OnNeedyActivation += NeedyStart;
		SOLVES = Bomb.GetSolvedModuleNames().Count();
    }

	void NeedyStart() {
		Debug.LogFormat("[M.A.S. #{0}] Security System Engaged. Standing Idle", moduleId);
		needyActive = true;
		LEDs[0].material = LightMat[1];
		SegmentDisplay.text = "ARMED";
		statusLAG = lagTime;
		/*string[] updatedBossList = BossInfo.GetIgnoredModules("42");
		foreach (string BOSS in updatedBossList){
			BOSSLIST.Add(BOSS);
		}*/

		string[] updatedBlackList = BossInfo.GetIgnoredModules("Forget Infinity");
		foreach (string BOSS in updatedBlackList){
			BLACKLIST.Add(BOSS);
		}

		foreach (KMSelectable NAME in Keypad) {
			KMSelectable pressedObject = NAME;
			NAME.OnInteract += delegate () { keypadPress(pressedObject); return false; };
		}
		ModeButton.OnInteract += delegate () { ModeSwitch(); return false; };
	}

	void ModeSwitch() {
		Audio.PlaySoundAtTransform("click51ALT", transform);
		LEDs[MODE].material = LightMat[0];
		MODE += 1;
		if (MODE == 3) {MODE = 0;}
		LEDs[MODE].material = LightMat[1];
		if(TRIPPED && Tflag && SubmitIndex != 0){
			SubmitIndex = 0;
			RenderDisplay();
		}
	}

	void keypadPress(KMSelectable KEY) {
		Audio.PlaySoundAtTransform("click30ALT", transform);
		int keyNum = Array.IndexOf(Keypad, KEY);
		//Debug.Log(keyNum);
		if(TRIPPED && Tflag){
			Audio.PlaySoundAtTransform("DigitEnter", transform);
			SubmitCode[SubmitIndex] = keyNum;
			SubmitIndex += 1;
			RenderDisplay();
			if (SubmitIndex == 5){CheckSolve();}
		}
	}

	void RenderDisplay() {
		string REND = "";
		for (int i = 0; i < SubmitIndex; i++) {
			REND += SubmitCode[i];
		}
		for (int i = SubmitIndex; i < 5; i++) {
			REND += "-";
		}
		SegmentDisplay.text = REND;
	}

	void Update() {
        if (SOLVES != Bomb.GetSolvedModuleNames().Count()) {
            GrabTrippedName();
			//Debug.Log(Tletter + " // " + MostRecent);

			if(SOLVES == Bomb.GetSolvableModuleIDs().Count()){
				SegmentDisplay.text = "*OFF*";
				statusLAG = lagTime;
			}

			if(!BLACKLIST.Contains(MostRecent) && !TRIPPED && needyActive){
				solveCount += 1;
				int COIN = UnityEngine.Random.Range(0, 2);
				//Debug.Log(SOLVES + " // " + Bomb.GetSolvableModuleIDs().Count());
				if (BOSSLIST.Contains(MostRecent) && SOLVES != Bomb.GetSolvableModuleIDs().Count()){// BOSS MODULES SHOULD ALWAYS ACTIVATE
					Debug.LogFormat("[M.A.S. #{0}] Security Module detected!", moduleId);
					solveCount = 0;
					TripCount += 1;
					ModuleTripped();
				} else if ((COIN == 1 || solveCount == 3) && SOLVES != Bomb.GetSolvableModuleIDs().Count()) {//COIN == 1
					solveCount = 0;
					TripCount += 1;
					ModuleTripped();
				}
			} else {
				if(BLACKLIST.Contains(MostRecent)){
					Debug.LogFormat("[M.A.S. #{0}] Module {1} is Blacklisted. Ignoring", moduleId, MostRecent);
				} else if (TRIPPED) {
					Debug.LogFormat("[M.A.S. #{0}] Alarm is currently tripped. Ignoring Module {1}", moduleId, MostRecent);
				} else {
					Debug.LogFormat("[M.A.S. #{0}] Alarm is deactivated. Ignoring Module {1}", moduleId, MostRecent);
				}
			}
        }

		if (needyActive) {
			TripCheck();
		}

		if (statusLAG != 0) {
			statusLAG -= 1;
			if (statusLAG == 1) {
				if(!TRIPPED){
					if(SOLVES == Bomb.GetSolvableModuleIDs().Count()){ SegmentDisplay.text = "     "; } else { SegmentDisplay.text = "*****"; }
					
				}
			}
		}
	}

	void ModuleTripped() {
		Audio.PlaySoundAtTransform("Alarm2", transform);
		string REND = "";
		int[] TempCode = {0, 0, 0, 0, 0};
		for (int i = 0; i < 5; i++){
			SolveCode[i] = UnityEngine.Random.Range(0, 10);
		}
		int ShufleCode = UnityEngine.Random.Range(0, 5);
		for (int i = 0; i < 5; i++){
			if (LetterCodes[ShufleCode, i] == 1){
				REND += AlphabetLET[AlphabetVAL[CODEletterPick[SolveCode[i], UnityEngine.Random.Range(0, 3)]]];
			} else {
				REND += SolveCode[i];
			}
			SolveCode[i] += Tnumber;
			if (SolveCode[i] > 9) {SolveCode[i] -= 10;}
		}

		if (BOSSLIST.Contains(MostRecent)) {
			SolveMODE = 2;
		} else if (CheckSerialMatch()){
			SolveMODE = 0;
		} else {
			SolveMODE = 1;
		}

		if (Tnumber % 2 == 0){ //         ONE
			TempCode[0] = SolveCode[0];
			SolveCode[0] = SolveCode[4];
			SolveCode[4] = TempCode[0];
		} else { //                       TWO
			TempCode[0] = SolveCode[1];
			SolveCode[1] = SolveCode[3];
			SolveCode[3] = TempCode[0];
		}
		if (true){
			for (int i = 0; i < 5; i++){
				TempCode[i] = SolveCode[i];
			}
			SolveCode[0] = TempCode[3];
			SolveCode[1] = TempCode[4];
			SolveCode[2] = TempCode[0];
			SolveCode[3] = TempCode[1];
			SolveCode[4] = TempCode[2];
		}
		if (TripCount % 2 == 0){ //       FOUR
			TempCode[0] = SolveCode[0];
			TempCode[1] = SolveCode[1];
			SolveCode[0] = SolveCode[4];
			SolveCode[1] = SolveCode[3];
			SolveCode[4] = TempCode[0];
			SolveCode[3] = TempCode[1];
		}

		if (SolveMODE == 2){ //           FIVE
			for (int i = 0; i < 5; i++){
				SolveCode[i] = invertList[SolveCode[i]];
			}
		}

		Debug.LogFormat("[M.A.S. #{0}] [ALARM TRIPPED BY {1}] Number of trips is [{2}] TRIP letter is [{3}] Display showing [{4}]", moduleId, MostRecent, TripCount, Tletter, REND);
		Debug.LogFormat("[M.A.S. #{0}] Solution Code [{1}{2}{3}{4}{5}] on MODE [{6}]", moduleId, SolveCode[0], SolveCode[1], SolveCode[2], SolveCode[3], SolveCode[4], SolveMODE+1);
		SegmentDisplay.text = REND;
		TRIPPED = true;
	}

	bool CheckSerialMatch() {
		string serialNum = Bomb.GetSerialNumber();
		for (int i = 0; i < 6; i++){
			if (serialNum[i].ToString() == Tletter || serialNum[i].ToString() == Tnumber.ToString()){return true;}
		}
		return false;
	}

	void GrabTrippedName () { //Borrowed from Validation
		MostRecent = GetLatestSolve(Bomb.GetSolvedModuleNames(), SolveList);
        SolveList.Add(MostRecent);
        MostRecent = SolveList[SOLVES];
        SOLVES = Bomb.GetSolvedModuleNames().Count();

		if(!TRIPPED && needyActive) {
			var module = MostRecent;
   	    	if (module.StartsWith("The ")) {
  	        	module = module.Substring(4);
        	}
			if (module.Contains("Cipher") || (module.Contains("Cycle") && !(module == "USA Cycle" || module == "Color-Cycle Button" || module == "Symbol Cycle" || module == "Light Cycle"))){
				BOSSLIST.Add(MostRecent);
			}
			if (!Regex.IsMatch(module.Substring(0, 1), "[a-zA-Z0-9]")){
				BLACKLIST.Add(MostRecent);
				return;
			}
			Tletter = module.Substring(0, 1).ToUpper();
			if(Regex.IsMatch(Tletter, "[0-9]")){
				Tnumber = Int32.Parse(Tletter);
			} else {
				Tnumber = AlphabetVAL[Array.IndexOf(AlphabetLET, Tletter)];
			}
		}
	}

	//Borrowed from Validation
	private string GetLatestSolve(List<string> a, List<string> b) {
        string z = "";
        for (int i = 0; i < b.Count; i++)
        {
            a.Remove(b.ElementAt(i));
        }

        z = a.ElementAt(0);
        return z;
    }
	//END*/

	void TripCheck() {
		//Debug.Log(Needy.GetNeedyTimeRemaining());
		if (TRIPPED && !Tflag){
			Needy.SetNeedyTimeRemaining(120);
			Tflag = true;
			LEDs[3].material = LightMat[3];
		} else if (!TRIPPED) {
			Needy.SetNeedyTimeRemaining(88);
			LEDs[3].material = LightMat[2];
		} else if (Needy.GetNeedyTimeRemaining() < 0.5f) {
			if(SOLVES == Bomb.GetSolvableModuleIDs().Count()) {
				SegmentDisplay.text = "DHORK";
				SubmitIndex = 0;
				TRIPPED = false;
				Tflag = false;
				Debug.LogFormat("[M.A.S. #{0}] Bomb diffused before alarm was disabled", moduleId);
			} else {
				SegmentDisplay.text = "ERROR";
				Needy.HandleStrike();
				statusLAG = lagTime;
				SubmitIndex = 0;
				TRIPPED = false;
				Tflag = false;
				Debug.LogFormat("[M.A.S. #{0}] Module Striked [Time Depleted] // Returning to Idle", moduleId);
			}
		}
	}

	void CheckSolve() {
		bool passed = true;
		for(int i = 0; i < 5; i++) {
			if (SubmitCode[i] != SolveCode[i] || MODE != SolveMODE){
				Needy.HandleStrike();
				passed = false;
				break;
			}
		}

		SubmitIndex = 0;
		TRIPPED = false;
		Tflag = false;
		if (passed){
			Audio.PlaySoundAtTransform("Confirm1", transform);
			SegmentDisplay.text = "*****";
			Debug.LogFormat("[M.A.S. #{0}] Submitted Code [{1}{2}{3}{4}{5}] on MODE [{6}] // Code Accepted", moduleId, SubmitCode[0], SubmitCode[1], SubmitCode[2], SubmitCode[3], SubmitCode[4], MODE+1);
		} else {
			SegmentDisplay.text = "ERROR";
			statusLAG = lagTime;
			Debug.LogFormat("[M.A.S. #{0}] Submitted Code [{1}{2}{3}{4}{5}] on MODE [{6}] // Incorrect Code [Module Striked]", moduleId, SubmitCode[0], SubmitCode[1], SubmitCode[2], SubmitCode[3], SubmitCode[4], MODE+1);
		}
		Debug.LogFormat("[M.A.S. #{0}] Returning to Idle", moduleId);
	}

		// Twitch Plays Support by Kilo Bites // Modified by Nimsay Ramsey

#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"!{0} Press 0-9 1-5 to push a number # times || !{0} Press Mode 1-3 to push the Mode button # times & clear the display";
#pragma warning restore 414

	bool isValidPos(string n)
	{
		string[] valids = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "MODE"};
		if (!valids.Contains(n))
		{
			return false;
		}
		return true;
	}

	IEnumerator ProcessTwitchCommand (string command)
	{
		yield return null;

		string[] split = command.ToUpperInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

		if (split[0].EqualsIgnoreCase("PRESS"))
		{
			int numberClicks = 0;
			int pos = 0;
			if (split.Length != 3)
			{
				yield return "sendtochaterror Please specify a button to press!";
				yield break;
			}
			else if (!isValidPos(split[1]))
			{
				yield return "sendtochaterror " + split[1] + " is not a valid button!";
				yield break;
			}
			if(split[1].EqualsIgnoreCase("MODE")){
				if (!"123".Any(x => split[2].Contains(x)))
				{
					yield return "sendtochaterror Range must be between 1-3!";
					yield break;
				}
				int.TryParse(split[2], out numberClicks);
				var presses = 0;
				while (presses != numberClicks)
				{
					ModeButton.OnInteract();
					presses++;
					yield return new WaitForSeconds(0.1f);
				}
			} else {
				if (!"12345".Any(x => split[2].Contains(x)))
				{
					yield return "sendtochaterror Range must be between 1-5!";
					yield break;
				}
				int.TryParse(split[1], out pos);
				int.TryParse(split[2], out numberClicks);

				var presses = 0;
				while (presses != numberClicks)
				{
					Keypad[pos].OnInteract();
					presses++;
					yield return new WaitForSeconds(0.1f);
				}
			}
			yield break;
		}
	}

	void TwitchHandleForcedSolve() { //Autosolver
		StartCoroutine(DealWithNeedy());
	}
	
	IEnumerator DealWithNeedy () {
		while (true) {
			while(!TRIPPED){
				yield return null;
			}
			while (MODE != SolveMODE || SubmitIndex > 0) {
				ModeButton.OnInteract();
				yield return new WaitForSeconds(0.1f);
			}
			for (int i = SubmitIndex; i < 5; i++){
				Keypad[SolveCode[SubmitIndex]].OnInteract();
				yield return new WaitForSeconds(0.1f);
			}
		}
	}

}
