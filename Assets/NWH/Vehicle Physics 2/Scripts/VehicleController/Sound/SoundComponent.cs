using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using System.Linq;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    /// <summary>
    ///     Base class for all vehicle SoundComponents.
    ///     Inserts a layer above Unity's AudioSource(s) which insures that the values are set properly, master volume is used,
    ///     etc.
    ///     Supports multiple AudioSources/AudioClips per one SoundComponent for random clip switching.
    /// </summary>
    [Serializable]
    public abstract class SoundComponent : VehicleComponent
    {
        /// <summary>
        ///     Base volume of the sound component.
        /// </summary>
        [FormerlySerializedAs("volume")]
        [Range(0f, 1f)]
        [Tooltip("    Base volume of the sound component.")]
        public float baseVolume = 0.1f;

        /// <summary>
        ///     List of audio clips this component can use. Some components can use multiple clips in which case they will be
        ///     chosen at random, and some components can use only one
        ///     in which case only the first clip will be selected. Check manual for more details.
        /// </summary>
        [Tooltip(
            "List of audio clips this component can use. Some components can use multiple clips in which case they will be chosen at random, and some components can use only one " +
            "in which case only the first clip will be selected. Check manual for more details.")]
        public List<AudioClip> clips = new List<AudioClip>();

        /// <summary>
        /// Audio source for this component.
        /// </summary>
        [Tooltip("Audio sources for this component. Can be multiple (e.g. multiple wheels per SkidComponent)")]
        [NonSerialized]
        public AudioSource source;

        public virtual bool InitializeWithNoClips
        {
            get { return false; }
        }

        public abstract GameObject ContainerGO { get; }

        public abstract AudioMixerGroup AudioMixerGroup { get; }

        public abstract int Priority { get; }


        /// <summary>
        ///     Gets or sets the first clip in the clips list.
        /// </summary>
        public AudioClip Clip
        {
            get { return clips.Count > 0 ? clips[0] : null; }
            set
            {
                if (clips.Count > 0)
                {
                    clips[0] = value;
                }
                else
                {
                    clips.Add(value);
                }
            }
        }


        protected override void VC_Initialize()
        {
            if (!InitializeWithNoClips && (clips == null || clips.Count == 0))
            {
                return;
            }

            source = CreateAndRegisterAudioSource(AudioMixerGroup, ContainerGO);
            if (source == null)
            {
                Debug.LogWarning($"AudioSource could not be created on {GetType().Name}! " +
                                 $"Make sure that the Project Settings > Audio > Disable Unity Audio is not ticked" +
                                 $" and that the mixer (if assigned) has the required audio groups.");
                return;
            }

            source.priority = Priority;

            if (InitPlayOnAwake)
            {
                Play();
            }
            else
            {
                Stop();
            }

            base.VC_Initialize();
        }


        protected AudioSource CreateAndRegisterAudioSource(AudioMixerGroup mixerGroup, GameObject container)
        {
            if (mixerGroup == null)
            {
                Debug.LogError($"Trying to setup an AudioSource with null mixer group on {vehicleController.name}.");
                return null;
            }

            if (container == null)
            {
                Debug.LogError("Trying to use a null container.");
                return null;
            }

            AudioSource source = container.AddComponent<AudioSource>();
            if (source == null)
            {
                Debug.LogError("Failed to create AudioSource.");
                return null;
            }

            source.outputAudioMixerGroup = mixerGroup;
            source.spatialBlend = InitSpatialBlend;
            source.playOnAwake = InitPlayOnAwake;
            source.loop = InitLoop;
            source.volume = InitVolume * vehicleController.soundManager.masterVolume;
            source.clip = InitClip;
            source.priority = 100;
            source.dopplerLevel = InitDopplerLevel;
            return source;
        }

        /// <summary>
        /// Override to set the initial source loop value.
        /// </summary>
        public virtual bool InitLoop
        {
            get { return false; }
        }

        /// <summary>
        /// Override to set the initial AudioClip value.
        /// </summary>
        public virtual AudioClip InitClip
        {
            get { return Clip; }
        }

        /// <summary>
        /// Override to set the initial source volume.
        /// </summary>
        public virtual float InitVolume
        {
            get { return baseVolume; }
        }

        /// <summary>
        /// Override to set the initial spatial blend.
        /// </summary>
        public virtual float InitSpatialBlend
        {
            get { return vehicleController.soundManager.spatialBlend; }
        }

        /// <summary>
        /// Override to set the initial doppler level.
        /// </summary>
        public virtual float InitDopplerLevel
        {
            get { return vehicleController.soundManager.dopplerLevel; }
        }

        /// <summary>
        /// Override to set the initial source play on awake setting.
        /// </summary>
        public virtual bool InitPlayOnAwake
        {
            get { return false; }
        }


        /// <summary>
        ///     Gets a random clip from clips list.
        /// </summary>
        public AudioClip RandomClip
        {
            get { return clips[Random.Range(0, clips.Count)]; }
        }


        /// <summary>
        ///     Enables all the AudioSources belonging to this SoundComponent.
        ///     Calls Play() on all the looping sources.
        /// </summary>
        public override bool VC_Enable(bool calledByParent)
        {
            if (base.VC_Enable(calledByParent))
            {
                source.enabled = true;
                return true;
            }

            return false;
        }


        /// <summary>
        ///     Disables all the AudioSources belonging to this SoundComponent.
        ///     Will call StopEngine() as well as disable the source.
        /// </summary>
        public override bool VC_Disable(bool calledByParent)
        {
            if (base.VC_Disable(calledByParent))
            {
                Stop();
                source.enabled = false;
                return true;
            }

            return false;
        }


        public virtual void PlayRandomClip()
        {
            Play(Random.Range(0, clips.Count));
        }


        public IEnumerator PlayForDurationCoroutine(int clipIndex, float duration)
        {
            Play(clipIndex);
            yield return new WaitForSeconds(duration);
            Stop();
        }


        public virtual void Play()
        {
            if (!source.enabled) return;

            if (source.isPlaying)
            {
                return;
            }
            source.Play();
        }


        /// <summary>
        ///     Plays the source at index.
        /// </summary>
        /// <param name="sourceIndex">Index of the source to play.</param>
        public virtual void Play(int clipIndex)
        {
            if (clipIndex >= 0 && clipIndex < clips.Count)
            {
                source.clip = clips[clipIndex];
            }
            else
            {
                return;
            }

            Play();
        }


        /// <summary>
        ///     Sets pitch for the first source in sources list.
        /// </summary>
        /// <param name="pitch">Pitch to set.</param>
        public virtual void SetPitch(float pitch)
        {
            pitch = pitch < 0 ? 0 : pitch > 5 ? 5 : pitch;
            source.pitch = pitch;
        }

        /// <summary>
        ///     Sets volume for the first source in sources list. Use instead of directly changing source volume as this takes
        ///     master volume into account.
        /// </summary>
        /// <param name="volume">Volume to set.</param>
        public virtual void SetVolume(float volume)
        {
            source.volume = volume * vehicleController.soundManager.masterVolume;
        }

        /// <summary>
        ///     Stops the AudioSource at index if already playing.
        /// </summary>
        /// <param name="index">Target AudioSource index.</param>
        public virtual void Stop()
        {
            if (!source.isPlaying)
            {
                return;
            }

            source.Stop();
        }


        public virtual void AddDefaultClip(string clipName)
        {
            Clip = Resources.Load(VehicleController.DEFAULT_RESOURCES_PATH + "Sound/" + clipName) as AudioClip;
            if (Clip == null)
            {
                Debug.LogWarning(
                    $"Audio Clip for sound component {GetType().Name} could not be loaded from resources. " +
                    $"Source will not play." +
                    $"Assign an AudioClip manually.");
            }
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [CustomPropertyDrawer(typeof(SoundComponent), true)]
    public partial class SoundComponentDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("baseVolume");
            drawer.ReorderableList("clips");

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
