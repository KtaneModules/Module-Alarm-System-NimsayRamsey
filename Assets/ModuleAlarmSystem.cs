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
	public KMNeedyModule Needy;
	public KMAudio Audio;

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
	private string[] BLACKLIST = new string[] {};

	private bool needyActive = false;
	//-----------------------------------------------------//
	
	void Awake() {
		//TRIPPED = true;
		GetComponent<KMNeedyModule>().OnNeedyActivation += NeedyStart;
		SOLVES = Bomb.GetSolvedModuleNames().Count();
    }

	void NeedyStart() {
		needyActive = true;
		LEDs[3].material = LightMat[3];
		LEDs[0].material = LightMat[1];
		SegmentDisplay.text = "*****";
		foreach (KMSelectable NAME in Keypad) {
			KMSelectable pressedObject = NAME;
			NAME.OnInteract += delegate () { keypadPress(pressedObject); return false; };
		}
		ModeButton.OnInteract += delegate () { ModeSwitch(); return false; };
	}

	void ModeSwitch() {
		Audio.PlaySoundAtTransform("click26", transform);
		LEDs[MODE].material = LightMat[0];
		MODE += 1;
		if (MODE == 3) {MODE = 0;}
		LEDs[MODE].material = LightMat[1];
	}

	void keypadPress(KMSelectable KEY) {
		Audio.PlaySoundAtTransform("click25", transform);
		int keyNum = Array.IndexOf(Keypad, KEY);
		Debug.Log(keyNum);
		if(TRIPPED && Tflag){
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

	void CheckSolve() {
		bool passed = true;
		for(int i = 0; i < 5; i++) {
			if (SubmitCode[i] != SolveCode[i]){
				Needy.HandleStrike();
				passed = false;
				break;
			}
		}

		SubmitIndex = 0;
		TRIPPED = false;
		Tflag = false;
		if (passed){
			SegmentDisplay.text = "*****";
		} else {
			SegmentDisplay.text = "ERROR";
		}
	}

	void Update() {
        if (SOLVES != Bomb.GetSolvedModuleNames().Count()) {
            GrabTrippedName();
			Debug.Log(Tletter + " // " + MostRecent);

			if(!BLACKLIST.Contains(MostRecent) && !TRIPPED && needyActive){
				solveCount += 1;
				int COIN = UnityEngine.Random.Range(0, 2);
				if (COIN == 1 || solveCount == 3) {//COIN == 1
					solveCount = 0;
					ModuleTripped();
				}
			}
        }

		if (needyActive) {
			TripCheck();
		}
	}

	void ModuleTripped() {
		string REND = "";
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
		
		Debug.Log(SolveCode[0] + "" + SolveCode[1] + "" + SolveCode[2] + "" + SolveCode[3] + "" + SolveCode[4]);
		SegmentDisplay.text = REND;
		TRIPPED = true;
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
			Tnumber = AlphabetVAL[Array.IndexOf(AlphabetLET, Tletter)];
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
			Needy.SetNeedyTimeRemaining(60);
			Tflag = true;
			TripCount += 1;
		} else if (!TRIPPED) { //Needy.GetNeedyTimeRemaining() != SelectedNumber
			Needy.SetNeedyTimeRemaining(88);
		} else if (Needy.GetNeedyTimeRemaining() < 0.5f) {
			Needy.HandleStrike();
			SubmitIndex = 0;
			TRIPPED = false;
			Tflag = false;
			SegmentDisplay.text = "ERROR";
		}
	}
}
