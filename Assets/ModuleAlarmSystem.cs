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
	private string[] BOSSLIST = {
		//Custom Additions
		"Turn The Keys", "Custom Keys",
		"The Swan",
		"Silo Authorization",
		"Scrabble Scramble",
		//Safe and Password Modules
		"Safety Safe",
		"The Jewel Vault",
		//End Solves
		"Forget Me Not",
		"Forget Everything",
		"Forget This",
		"Forget Them All",
		"Forget Enigma",
		"Forget Us Not",
		"Forget Perspective",
		"Forget Infinity"
	};
	private string[] BLACKLIST = {
		"Doomsday Button",
		"Castor",
		"Pollux",
		"X",
		"Y"
	};

	private bool needyActive = false;
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
		Debug.LogFormat("[ModuleAlarmSystem #{0}] Security System Engaged. Standing Idle", moduleId);
		needyActive = true;
		LEDs[0].material = LightMat[1];
		SegmentDisplay.text = "*****";
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

			if(!BLACKLIST.Contains(MostRecent) && !TRIPPED && needyActive){
				solveCount += 1;
				int COIN = UnityEngine.Random.Range(0, 2);
				if (COIN == 1 || solveCount == 3) {//COIN == 1
					solveCount = 0;
					TripCount += 1;
					ModuleTripped();
				}
			} else {
				Debug.LogFormat("[ModuleAlarmSystem #{0}] Module {1} is Blacklisted. Ignoring", moduleId, MostRecent);
			}
        }

		if (needyActive) {
			TripCheck();
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

		Debug.LogFormat("[ModuleAlarmSystem #{0}] [ALARM TRIPPED BY {1}] Number of trips is [{2}] TRIP letter is [{3}] Display showing [{4}]", moduleId, MostRecent, TripCount, Tletter, REND);
		Debug.LogFormat("[ModuleAlarmSystem #{0}] Solution Code [{1}{2}{3}{4}{5}] on MODE [{6}]", moduleId, SolveCode[0], SolveCode[1], SolveCode[2], SolveCode[3], SolveCode[4], SolveMODE+1);
		SegmentDisplay.text = REND;
		TRIPPED = true;
	}

	bool CheckSerialMatch() {
		string serialNum = Bomb.GetSerialNumber();
		for (int i = 0; i < 6; i++){
			if (serialNum[i].ToString() == Tletter){return true;}
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
		} else if (!TRIPPED) { //Needy.GetNeedyTimeRemaining() != SelectedNumber
			Needy.SetNeedyTimeRemaining(88);
			LEDs[3].material = LightMat[2];
		} else if (Needy.GetNeedyTimeRemaining() < 0.5f) {
			Needy.HandleStrike();
			SubmitIndex = 0;
			TRIPPED = false;
			Tflag = false;
			SegmentDisplay.text = "ERROR";
			Debug.LogFormat("[ModuleAlarmSystem #{0}] Module Striked [Time Depleted] // Returning to Idle", moduleId);
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
			Debug.LogFormat("[ModuleAlarmSystem #{0}] Submitted Code [{1}{2}{3}{4}{5}] on MODE [{6}] // Code Accepted", moduleId, SubmitCode[0], SubmitCode[1], SubmitCode[2], SubmitCode[3], SubmitCode[4], MODE+1);
		} else {
			SegmentDisplay.text = "ERROR";
			Debug.LogFormat("[ModuleAlarmSystem #{0}] Submitted Code [{1}{2}{3}{4}{5}] on MODE [{6}] // Incorrect Code [Module Striked]", moduleId, SubmitCode[0], SubmitCode[1], SubmitCode[2], SubmitCode[3], SubmitCode[4], MODE+1);
		}
		Debug.LogFormat("[ModuleAlarmSystem #{0}] Returning to Idle", moduleId);
	}
}
