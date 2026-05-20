# Annotation Guide for AI-Based Detection of Stimming Behaviors

## 1. Introduction and Objectives
Self-stimulatory behavior, commonly referred to as **stimming**, consists of repetitive, stereotyped physical movements, vocalizations, or object manipulations. While historically associated primarily with Autism Spectrum Disorder (ASD), stimming is a universal human regulatory mechanism used to manage sensory overload, regulate emotional states (anxiety, excitement, stress), or maintain focus.

For a multimodal AI model utilizing computer vision (CV) and audio processing, detecting stimming presents substantial technical challenges:
* **High Intra-class Variance:** A single behavioral category (e.g., "hand flapping") manifests with drastically different frequencies, amplitudes, and spatial paths across individuals.
* **Context Dependency:** The same physical movement (e.g., clapping) can be a normative social response or a self-regulatory stim depending on context, duration, and triggering environmental stimuli.
* **Data Sparsity & Occlusion:** Movements may occur out of frame, be partially obscured by furniture, or be muted by background ambient noise.

This document establishes rigorous, multi-modal definitions, structural taxonomies, and precise heuristic rules to guide data annotation, model training, and false-positive mitigation for an AI-driven behavioral detection pipeline.

---

## 2. Structural Taxonomy of Stimming Behaviors

Stimming behaviors are categorized by their primary sensory-motor modalities. For machine learning deployment, these are split into **Visual/Motor (Video)**, **Acoustic/Vocal (Audio)**, and **Tactile/Object-Oriented (Multimodal)** dimensions.

```
                                 [Stimming Behaviors]
                                          |
        +---------------------------------+---------------------------------+
        |                                 |                                 |
[Motor / Visual]                  [Acoustic / Vocal]             [Tactile / Object]
   (Video Stream)                   (Audio Stream)               (Multimodal Stream)
        |                                 |                                 |
        +-- Hand Flapping/Waving          +-- Echolalia                     +-- Object Spinning
        +-- Body Rocking                  +-- Repetitive Vocalizations      +-- Tactile Rubbing
        +-- Finger Flicking/Tapping       +-- Rhythmic Clicking/Clapping    +-- Skin Picking/Scratching
        +-- Pacing / Tip-toeing
```

---

## 3. Multimodal Behavioral Specifications

### 3.1 Motor / Visual Stimming (Video Pipeline)

#### 3.1.1 Hand Flapping / Waving
* **Anatomical Targets:** Wrist joints, metacarpophalangeal joints, forearm tracking.
* **Kinematic / Spatiotemporal Profiles:**
    * **Frequency:** High-frequency, repetitive oscillations ranging between $3.0	ext{ Hz}$ and $7.0	ext{ Hz}$.
    * **Trajectory:** Predominantly vertical (up-and-down flapping) or horizontal (side-to-side waving) sinusoidal-like paths.
    * **Amplitude:** Variable; ranges from micro-flaps restricted to wrist flexion to macro-flaps involving shoulder abduction.
* **Edge Cases & Ambiguities:**
    * *Social Waving:* Characterized by a lower frequency ($<2.0	ext{ Hz}$), shorter duration ($<3	ext{ seconds}$), and clear vector orientation toward an interlocutor.
    * *Expressive Gesturing:* Tied dynamically to conversational speech cadence, exhibiting lower periodicity and non-uniform trajectories.
* **Annotation Exclusion Criteria:** Do not annotate if the motion occurs strictly as a greeting or part of an explicit interactive game (e.g., "pat-a-cake").

#### 3.1.2 Body Rocking
* **Anatomical Targets:** Torso vector (sternum to pelvis line), head positioning relative to vertical axis.
* **Kinematic / Spatiotemporal Profiles:**
    * **Frequency:** Low-frequency, highly rhythmic oscillations between $0.5	ext{ Hz}$ and $1.5	ext{ Hz}$.
    * **Trajectory:** Continuous anterior-posterior (forward-backward) or lateral (side-to-side) angular displacement.
    * **Amplitude:** Angle of torso displacement exceeds $15^\circ$ from a neutral sitting or standing posture.
* **Edge Cases & Ambiguities:**
    * *Functional Adjustments:* Shifting posture to achieve physical comfort; typically completes within 1-2 cycles and lacks strict periodicity.
    * *Laughing / Crying:* Torso displacement during extreme emotion; isolated by looking for accompanying non-periodic facial expressions or explosive vocal signals.
* **Annotation Exclusion Criteria:** Exclude movements directly induced by external mechanical vectors (e.g., sitting on a rocking chair, riding in a moving vehicle).

#### 3.1.3 Finger Flicking / Tapping
* **Anatomical Targets:** Distal phalanges, interphalangeal joints.
* **Kinematic / Spatiotemporal Profiles:**
    * **Frequency:** Rapid, erratic, or highly periodic movements exceeding $4.0	ext{ Hz}$.
    * **Trajectory:** Flicking of fingers within the visual field of the individual, or rapid percussive tapping against thumbs or nearby surfaces.
* **Edge Cases & Ambiguities:**
    * *Keyboard Typing / Musical Play:* Goal-directed motor activities. Differentiated by tracking object proximity (keyboard, instrument) and functional hand configuration.
* **Annotation Exclusion Criteria:** Exclude standard typing, counting on fingers, or instrumental manipulation.

#### 3.1.4 Pacing / Tip-toeing
* **Anatomical Targets:** Ankle joint extension, calcaneus-to-floor distance, global bounding box displacement.
* **Kinematic / Spatiotemporal Profiles:**
    * **Trajectory:** Repetitive linear or circular pathing within a confined space. Tip-toeing is characterized by a sustained elevation of the calcaneus (heel) during the stance phase of locomotion.
* **Edge Cases & Ambiguities:**
    * *Goal-Directed Walking:* Trajectory shows a clear, non-repetitive path from Point A to Point B.
* **Annotation Exclusion Criteria:** Linear transit through an environment with a discernible spatial destination.

---

### 3.2 Acoustic / Vocal Stimming (Audio Pipeline)

#### 3.2.1 Echolalia (Immediate & Delayed)
* **Acoustic / Spectral Profiles:**
    * **Waveform:** Literal, phonetic repetition of words, phrases, or environmental sounds.
    * **Prosody:** Often exhibits flat, mechanical, or sing-song intonation patterns that match the source audio clip exactly, disregarding native conversational prosody.
* **Edge Cases & Ambiguities:**
    * *Conversational Affirmation:* Repeating a word to show agreement (e.g., Person A: "It's cold." Person B: "Cold, exactly."). Differentiated by standard conversational latency ($<500	ext{ ms}$) and conversational turn-taking dynamics.
* **Annotation Exclusion Criteria:** Normative verbal confirmation or interactive language practice.

#### 3.2.2 Repetitive Vocalizations (Humming, Groaning, Shrill Noises)
* **Acoustic / Spectral Profiles:**
    * **Spectral Features:** Sustained fundamental frequency ($F_0$) with highly stable harmonic structures (for humming) or sudden, high-amplitude impulse spikes in the $2	ext{ kHz} - 8	ext{ kHz}$ range (for shrill vocal bursts).
    * **Duration:** Continuous humming lasting $>3	ext{ seconds}$, or brief burst vocalizations repeating with a standard period.
* **Edge Cases & Ambiguities:**
    * *Vocal Clearance / Coughing:* Brief, non-harmonic, turbulent noise profiles.
    * *Musical Singing:* Characterized by shifting musical intervals, scalar progression, and tracking linguistic phonemes.
* **Annotation Exclusion Criteria:** Standard singing with lexical progression, crying, coughing, or clearing of the throat.

---

### 3.3 Tactile / Object-Oriented Stimming (Multimodal Pipeline)

#### 3.3.1 Object Spinning / Rolling
* **Anatomical & Object Targets:** Hand bounding box intersections with rigid objects (e.g., wheels, coins, pens).
* **Kinematic / Spectral Profiles:**
    * Continuous rotational velocity applied to an object or repetitive back-and-forth translational rolling across a surface, persisting for more than $5	ext{ seconds}$.
* **Edge Cases & Ambiguities:**
    * *Functional Toy Play:* Using a toy car as intended by rolling it across a track with variable speed and trajectory.
* **Annotation Exclusion Criteria:** Intended, non-stereotyped functional use of a tool or toy object.

#### 3.3.2 Tactile Rubbing / Picking
* **Anatomical Targets:** Hand-to-hand interaction, hand-to-skin surface contact, or hand-to-fabric interfaces.
* **Kinematic / Spectral Profiles:**
    * Micro-movements with highly repetitive spatial paths, often generating low-amplitude friction sounds detectable via high-gain close-proximity microphones.

---

## 4. Multi-Modal Feature Matrix for Machine Learning

| Behavior ID | Behavior Class | Primary Modality | CV Features (Video) | Audio Features | Critical Thresholds |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **STM-01** | Hand Flapping | Video | Optical Flow Vectors, 2D/3D Hand Skeleton Keypoint Oscillations | N/A (or low-amplitude whooshing noise) | Freq: $3.0 - 7.0	ext{ Hz}$<br>Duration: $\ge 2.0	ext{ s}$ |
| **STM-02** | Body Rocking | Video | Torso Angle Displacement, Rigid Bounding Box Cyclical Shifts | N/A (or rhythmic surface friction noise) | Freq: $0.5 - 1.5	ext{ Hz}$<br>Duration: $\ge 5.0	ext{ s}$<br>Angle: $>15^\circ$ |
| **STM-03** | Echolalia | Audio | Minimal or uncoordinated lip movement relative to audio track | Spectral Cross-Correlation with antecedent audio, Pitch Contour Matching | Match Metric: $>85\%$ spectral cross-correlation<br>Latency: $\le 2.0	ext{ s}$ (Immediate) |
| **STM-04** | Repetitive Vocal | Audio | N/A | Highly stable $F_0$ harmonics, constant Mel-Frequency Cepstral Coefficients (MFCCs) | Duration: $\ge 3.0	ext{ s}$ (Humming)<br>Periodicity: $\ge 3$ iterations within $10	ext{ s}$ (Bursts) |
| **STM-05** | Object Spinning | Multimodal | Object Rotation Vector tracking, Contact Point Invariance | Periodic high-frequency mechanical friction sounds | Duration: $\ge 5.0	ext{ s}$<br>Angular Velocity: Constant ($\pm 10\%$) |

---

## 5. Instructions for Dataset Annotation and Accuracy

To establish a high-fidelity ground truth dataset, human annotators must adhere to strict bounding rules.

### 5.1 Temporal Windowing Instructions
* **Onset Marker (Start Timestamp):** Record the exact frame where the kinetic or acoustic pattern initiates its first periodic cycle. For vocalizations, this is the first frame where the audio signal rises $6	ext{ dB}$ above the ambient noise floor.
* **Offset Marker (End Timestamp):** Record the frame where the behavior completely breaks rhythm, drops below critical amplitude thresholds, or shifts into a distinct functional activity for a duration exceeding $1.0	ext{ second}$.
* **Fragmented Behaviors:** If a behavior stops for less than $1.0	ext{ second}$ and immediately resumes, annotate it as a **single continuous event** to prevent training the model to predict artificial segment fragmentations.

### 5.2 Inter-Rater Reliability (IRR) Protocols
* All validation data subsets must be double-blind annotated by at least two independent review layers.
* **Acceptance Metric:** Cohen’s Kappa ($\kappa$) or Fleiss’ Kappa must reach $\ge 0.85$ for temporal overlap (Intersection over Union $	ext{IoU} \ge 0.75$) before a sequence is ingested into the gold-standard training tier.

---

## 6. Strategic Mitigation of False Positives

To minimize false-positive rates ($FPR$) when deploying models into real-world environments, developers must implement the following architectural constraints and validation filters:

### 6.1 Spatial and Contextual Masking
* **Environmental Static Masking:** If a subject is placed in a setting with mechanical oscillators (e.g., fans, window blinds blowing in wind), apply static spatial masks or utilize background subtraction layers to prevent optical flow artifacts from falsifying high-frequency hand flapping or rocking signatures.
* **Functional Activity Filters:** De-weight or mute behavioral triggers when specific contextual detectors are active. For example, if an object classifier detects a computer keyboard with a confidence score $>0.85$, the threshold for triggering a `STM-03` (Finger Tapping) classification must be automatically scaled up by a factor of $2.5	imes$.

### 6.2 Temporal Heuristics and Multi-Frame Validation
* **Minimum Duration Hard Constraints:** Casual social gestures or structural adjustments are transient. Enforce hard temporal constraints at the classifier's head layer. Any detected sequence that satisfies kinetic frequency criteria but fails to sustain the behavior across the minimum duration threshold (e.g., $2.0	ext{ seconds}$ for hand flapping) must be discarded.
* **Periodicity Verification:** Implement a Fast Fourier Transform (FFT) or Autocorrelation function over the temporal tracking vectors of joint coordinates. True stimming exhibits high spectral energy concentrated within narrow frequency bands. If the structural movement exhibits a chaotic, broad-spectrum energy profile, it should be categorized as an un-patterned functional movement rather than a stim.

```
[Tracking Coordinates] ---> [FFT Analysis] ---> Clear Spectral Peak? ---> YES ---> Classify as Stim
                                             ---> NO  (Broadband Noise) ---> DISMISS (Functional Movement)
```

### 6.3 Multimodal Cross-Verification (Sensor Fusion)
* **Audio-Visual Gating:** Implement multi-modal verification gating for specific categories. For instance, rhythmic clapping or tapping events must be cross-verified by finding temporal correspondence between the visual contact frame and an explosive audio impulse peak ($\Delta t \le 33	ext{ ms}$). If a video stream displays hand motions resembling clapping but the audio pipeline records no corresponding acoustic pulses, the event must be marked as an artifact or secondary movement, preventing false identification.