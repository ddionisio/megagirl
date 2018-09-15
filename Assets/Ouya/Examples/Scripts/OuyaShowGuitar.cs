/*
 * Copyright (C) 2012, 2013 OUYA, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using Object=UnityEngine.Object;
using Random = UnityEngine.Random;

public class OuyaShowGuitar : MonoBehaviour,
    OuyaSDK.IPauseListener, OuyaSDK.IResumeListener,
    OuyaSDK.IMenuButtonUpListener,
    OuyaSDK.IMenuAppearingListener
{
    [Serializable]
    public class CubeLaneItem
    {
        public GameObject StartPosition = null;
        public GameObject EndPosition = null;
        public Color LaneColor = Color.green;
        public OuyaSDK.KeyEnum LaneButton = OuyaSDK.KeyEnum.HARMONIX_ROCK_BAND_GUITAR_GREEN;
        public GameObject Instance = null;
        public AudioSource LaneSound = null;
    }

    public List<CubeLaneItem> Lanes = new List<CubeLaneItem>();

    [Serializable]
    public class NoteItem
    {
        public CubeLaneItem Parent = null;
        public GameObject Instance = null;
        public DateTime StartTime = DateTime.MinValue;
        public DateTime EndTime = DateTime.MinValue;
        public DateTime FadeTime = DateTime.MinValue;
        public bool UseLower = false;
    }

    private List<NoteItem> Notes = new List<NoteItem>();

    private int NoteTimeToLive = 4000;

    private int NoteTimeToCreate = 100;

    private int NoteTimeToFade = 250;

    private Dictionary<OuyaSDK.KeyEnum, bool> LastPressed = new Dictionary<OuyaSDK.KeyEnum, bool>();

    private DateTime m_timerCreate = DateTime.MinValue;

    private float LastStrum = 0f;

    public Transform TrackEnd = null;

    void Awake()
    {
        OuyaSDK.registerMenuButtonUpListener(this);
        OuyaSDK.registerMenuAppearingListener(this);
        OuyaSDK.registerPauseListener(this);
        OuyaSDK.registerResumeListener(this);
    }

    void OnDestroy()
    {
        OuyaSDK.unregisterMenuButtonUpListener(this);
        OuyaSDK.unregisterMenuAppearingListener(this);
        OuyaSDK.unregisterPauseListener(this);
        OuyaSDK.unregisterResumeListener(this);

        DestroyNotes();
    }

    public void OuyaMenuButtonUp()
    {
        Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().ToString());
    }

    public void OuyaMenuAppearing()
    {
        Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().ToString());
    }

    public void OuyaOnPause()
    {
        Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().ToString());
    }

    public void OuyaOnResume()
    {
        Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().ToString());
    }

    public void DestroyNote(NoteItem note)
    {
        if (note.Instance)
        {
            Object.DestroyImmediate(note.Instance, true);
            note.Instance = null;
        }
    }

    public void DestroyNotes()
    {
        foreach (NoteItem note in Notes)
        {
            DestroyNote(note);
        }
        Notes.Clear();
    }

    public void CreateNote(CubeLaneItem item)
    {
        NoteItem note = new NoteItem();
        note.StartTime = DateTime.Now;
        note.EndTime = DateTime.Now + TimeSpan.FromMilliseconds(NoteTimeToLive);
        note.Parent = item;
        note.Instance = (GameObject)Instantiate(item.StartPosition);
        (note.Instance.GetComponent<Renderer>() as MeshRenderer).material.color = item.LaneColor;
        if (0 == Random.Range(0, 10))
        {
            note.UseLower = true;
            note.Instance.transform.rotation = Quaternion.Euler(0, 15, 0);
        }
        Notes.Add(note);
    }

    void Update()
    {
        if (m_timerCreate < DateTime.Now)
        {
            m_timerCreate = DateTime.Now + TimeSpan.FromMilliseconds(NoteTimeToCreate);
            int index = Random.Range(0, Lanes.Count);
            CreateNote(Lanes[index]);
        }

        bool lower = OuyaExampleCommon.GetButton(OuyaSDK.KeyEnum.HARMONIX_ROCK_BAND_GUITAR_LOWER,
                                                 OuyaSDK.OuyaPlayer.player1);

        float strum = OuyaExampleCommon.GetAxis(OuyaSDK.KeyEnum.HARMONIX_ROCK_BAND_GUITAR_STRUM,
                                                OuyaSDK.OuyaPlayer.player1);

        bool strumChanged = LastStrum != strum;
        LastStrum = strum;

        foreach (CubeLaneItem item in Lanes)
        {
            // cache the button state
            LastPressed[item.LaneButton] = OuyaExampleCommon.GetButton(item.LaneButton, OuyaSDK.OuyaPlayer.player1);
        }

        List<NoteItem> removeList = new List<NoteItem>();
        foreach (NoteItem note in Notes)
        {
            if (note.EndTime < DateTime.Now)
            {
                removeList.Add(note);
                continue;
            }
            
            float elapsed = (float)(DateTime.Now - note.StartTime).TotalMilliseconds;
            
            note.Instance.transform.position =
                Vector3.Lerp(
                    note.Parent.StartPosition.transform.position,
                    note.Parent.EndPosition.transform.position,
                    elapsed/(float) NoteTimeToLive);

            bool inRange = Mathf.Abs(TrackEnd.position.z - note.Instance.transform.position.z) <= 16;
            bool afterRange = (note.Instance.transform.position.z - 8) < TrackEnd.position.z;
            if (inRange)
            {
                (note.Instance.GetComponent<Renderer>() as MeshRenderer).material.color = Color.white;
            }
            else if (afterRange)
            {
                (note.Instance.GetComponent<Renderer>() as MeshRenderer).material.color = new Color(0, 0, 0, 0.75f);
            }

            // use available press of the lane button
            if (LastPressed.ContainsKey(note.Parent.LaneButton) &&
                LastPressed[note.Parent.LaneButton])
            {
                if (
                    //check if note is across the finish line
                    inRange && 

                    //check if lower was used
                    (!note.UseLower ||
                    lower == note.UseLower) &&

                    // check if strum was used
                    strumChanged &&
                    strum != 0f)
                {
                    //use button press
                    LastPressed[note.Parent.LaneButton] = false;

                    //hit the note
                    if (note.FadeTime == DateTime.MinValue)
                    {
                        note.FadeTime = DateTime.Now + TimeSpan.FromMilliseconds(NoteTimeToFade);
                        note.Parent.LaneSound.volume = 1;
                    }
                }
            }

            if (note.FadeTime != DateTime.MinValue)
            {
                if (note.FadeTime < DateTime.Now)
                {
                    removeList.Add(note);
                    continue;
                }
                elapsed = (float)(note.FadeTime - DateTime.Now).TotalMilliseconds;
                (note.Instance.GetComponent<Renderer>() as MeshRenderer).material.color = Color.Lerp(note.Parent.LaneColor, Color.clear, 1f - elapsed / (float)NoteTimeToFade);
                note.Instance.transform.localScale = Vector3.Lerp(note.Instance.transform.localScale, note.Parent.StartPosition.transform.localScale * 2, elapsed / (float)NoteTimeToFade);

            }
        }
        foreach (NoteItem note in removeList)
        {
            Notes.Remove(note);
            DestroyNote(note);
        }
    }

    void FixedUpdate()
    {
        foreach (CubeLaneItem item in Lanes)
        {
            item.LaneSound.volume = Mathf.Lerp(item.LaneSound.volume, 0, Time.fixedDeltaTime);
        }
    }
}