using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using KModkit;
using System;
using System.Text.RegularExpressions;
using Random = UnityEngine.Random;

public class fourLightsScript : MonoBehaviour {

	public KMBombInfo bomb;
	public KMAudio sfx;
	public KMBombModule module;

	public KMSelectable[] labels;
	public Material[] litUnlit;
	public MeshRenderer[] bulbsMat;
	public TextMesh[] labelTexts;
	public Material solvedBulb;
	public KMGameCommands service;

	string[] labelsLabels = new string[4] {"1","2","3","4"}; //Labels from left to right
	int baseTenA;
	int baseTenB;
	int[] binaryAArray = new int[4]; //On/off from left to right
	int[] binaryBArray = new int[4];
	int numLit;
	List<string> solution = new List<string>();
	int stagetime = 0;
	bool firstPress = true;

	//Logging
	static int moduleIdCounter = 1;
	int moduleId;
	private bool moduleSolved;

	void Awake()
    {

    }

	// Use this for initialization
	void Start () {
		moduleId = moduleIdCounter++;
		foreach(KMSelectable label in labels)
			label.OnInteract += delegate () { PressButton(label); ; return false; };
		GenerateEnigma();
	}

	private void GenerateSolution()
	{
		if(baseTenA== 15)
		{
			Debug.LogFormat("[Four Lights #{0}] Using rule 1 : Press labels in ascending order.", moduleId);

			solution.AddRange(new string[] { "1", "2", "3", "4" });
		}
		else if (baseTenA == 0)
		{
			Debug.LogFormat("[Four Lights #{0}] Using rule 2 : Press labels in descending order.", moduleId);

			solution.AddRange(new string[] { "4", "3", "2", "1" }) ;
		}
		else if(numLit==bomb.GetOnIndicators().Count() && 4 - numLit==bomb.GetOffIndicators().Count()) //4-lit is unlit 
		{
			Debug.LogFormat("[Four Lights #{0}] Using rule 3 : Press position for lit and label for unlit.", moduleId);

			solution.AddRange(new string[] { labelsLabels[numLit-1],(4-numLit).ToString()});
		}
		else if (numLit == 1)
		{
			Debug.LogFormat("[Four Lights #{0}] Using rule 4 : Press the xth position", moduleId);

			solution.Add((numLit == 1 ? Array.IndexOf(binaryAArray, 1) + 1 : Array.IndexOf(binaryAArray, 0) + 1).ToString());
		}
		else if (baseTenA==baseTenB)
		{
			Debug.LogFormat("[Four Lights #{0}] Using rule 5 : Press the leftmost light, then the lowest label.", moduleId);

			solution.AddRange(new string[] { labelsLabels[Array.IndexOf(binaryAArray, 1)], (Array.IndexOf(binaryBArray,1)+1).ToString()});
		}
		else if (baseTenA>=10 && baseTenB >= 10) { 
			Debug.LogFormat("[Four Lights #{0}] Using rule 6 : Apply an AND logic gate and press position for 1s.", moduleId);

			for (int i = 0; i <= 3; i++)
				if (binaryAArray[i] == 1 && binaryBArray[i] == 1) solution.Add(labelsLabels[i]);	
		}
		else
		{
			Debug.LogFormat("[Four Lights #{0}] Using rule 7 : Press lit in ascending order and unlit in descending order.", moduleId);

			List<int> unlitList = new List<int>();
			List<int> litList = new List<int>();
			for(int i = 0; i < 4; i++)
			{
				
				switch (binaryAArray[i])
				{
					case 0:
						unlitList.Add(Int16.Parse(labelsLabels[i]));
						break;
					case 1:
						litList.Add(Int16.Parse(labelsLabels[i]));
						break;
					default:
						throw new IndexOutOfRangeException();
				}
			}
			unlitList.Sort(); unlitList.Reverse();
			litList.Sort();
			foreach (int lit in litList) solution.Add(lit.ToString());
			foreach (int unlit in unlitList) solution.Add(unlit.ToString());

		}		
		Debug.LogFormat("[Four Lights #{0}] Press the labels in this order : {1}", moduleId, solution.Join(","));
	}

	private void GenerateEnigma()
	{
		labelsLabels.Shuffle();
		baseTenA = baseTenB = numLit = 0;
		solution.Clear();
		foreach (TextMesh lblTxt in labelTexts)
			lblTxt.text = labelsLabels[Array.IndexOf(labelTexts, lblTxt)];
		foreach (MeshRenderer bulb in bulbsMat)
		{
			int bulbPlace = Array.IndexOf(bulbsMat, bulb);
			int unlitOrLit = Random.Range(0, 2);
			if (unlitOrLit == 1) numLit++;
			bulb.material = litUnlit[unlitOrLit];
			baseTenA += unlitOrLit * (int)(Math.Pow(2, 3 - bulbPlace));
			baseTenB += unlitOrLit * (int)(Math.Pow(2, 4 - Int16.Parse(labelsLabels[bulbPlace])));
			binaryAArray[bulbPlace] = unlitOrLit;
			binaryBArray[Int16.Parse(labelsLabels[bulbPlace])-1] =unlitOrLit;
		}
		Debug.LogFormat("[Four Lights #{0}] Labels are in this order : {1}", moduleId, labelsLabels.Join(","));
		Debug.LogFormat("[Four Lights #{0}] In base 10 : A={1} B={2}", moduleId, baseTenA, baseTenB);
		Debug.LogFormat("[Four Lights #{0}] In base 2 : A={1} B={2}", moduleId, binaryAArray.Join(""), binaryBArray.Join(""));
		GenerateSolution();
	}

	private void PressButton(KMSelectable label)
	{
		if (!moduleSolved)
		{
			label.AddInteractionPunch(.5f);
			sfx.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress,label.transform);
			int placement = Array.IndexOf(labels, label);
			if (labelsLabels[placement] == solution[stagetime])
			{
				Debug.LogFormat("[Four Lights #{0}] You pressed label {1} correctly.", moduleId, labelsLabels[placement]);
				if (firstPress)
				{
					for (int i = 0; i < 4; i++)
					{
						bulbsMat[i].material = litUnlit[0];
						labelTexts[i].text = null;
					}
				}
				stagetime++;
				if (stagetime == solution.Count())
				{
					Debug.LogFormat("[Four Lights #{0}] Module solved!.", moduleId);
					moduleSolved = true;
					sfx.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, label.transform);
					module.HandlePass();
					Victory();
				}
			}
			else
			{
				Debug.LogFormat("[Four Lights #{0}] You pressed label {1} when you were supposed to press {2}. Strike and reset!", moduleId, labelsLabels[placement],solution[stagetime]);
				InCaseOfStrike();
				module.HandleStrike();
				//service.CauseStrike("Can't binary properly");
			}		
		}

	}

	private void Victory()
	{
		foreach (MeshRenderer bulb in bulbsMat)
			bulb.material = solvedBulb;
		labelTexts[0].text = labelTexts[1].text = "G";
		labelTexts[2].text = labelTexts[3].text = "!";
	}

	private void InCaseOfStrike()
	{
		firstPress = true;
		stagetime = 0;
		foreach (TextMesh lblTxt in labelTexts)
			lblTxt.text = labelsLabels[Array.IndexOf(labelTexts, lblTxt)];
		foreach (MeshRenderer bulb in bulbsMat)
			bulb.material = litUnlit[binaryAArray[Array.IndexOf(bulbsMat, bulb)]];
	}

#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"!{0} position 2 3, !{0} pos 2 3, !{0} p 2 3 [2nd then 3rd position] | !{0} label 1 4, !{0} lab 1 4, !{0} l 1 4 [label 1 then 4] |";
#pragma warning restore 414

	KMSelectable[] ProcessTwitchCommand (string command)
	{
		var m = Regex.Match(command, @"^\s*(?:position|pos|p)\s+(([1-4] ?){1,4})\s*$");
		command = command.ToLowerInvariant();
		if (m.Success)
		{
			List<KMSelectable> ans = new List<KMSelectable>();
			foreach (char tap in m.Groups[1].Value)
			{
				if (tap != ' ')
					ans.Add(labels[(int)char.GetNumericValue(tap) - 1]);
			}

			return ans.ToArray();
		}
		m = Regex.Match(command, @"^\s*(?:label|lab|l)\s+(([1-4] ?){1,4})\s*$");
		if (m.Success)
		{
			List<KMSelectable> ans = new List<KMSelectable>();
			foreach (var tap in m.Groups[1].Value)
			{
				Debug.LogFormat(char.GetNumericValue(tap).ToString());
				Debug.LogFormat(Array.IndexOf(labelsLabels, char.GetNumericValue(tap).ToString()).ToString());
				if(tap!=' ')
					ans.Add(labels[Array.IndexOf(labelsLabels, char.GetNumericValue(tap).ToString())]);
			}
				
			return ans.ToArray();
		}
		return null;
	}

	IEnumerator TwitchHandleForcedSolve()
	{
		foreach(string step in solution)
		{
			labels[Array.IndexOf(labelsLabels, step)].OnInteract();
			yield return new WaitForSeconds(.1f);
		}
	}
}
