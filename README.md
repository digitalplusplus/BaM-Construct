What is this project
====================
This is a fully featured, free*, functional <b>UNITY 6 MULTIPLAYER VR</b> project. Features:
* OPENXR: so in principle platform-agnostic
* AI POWERED NPC: NPC with speech interaction: using STT+LLM+TTS public AI cloud services - ReadyToRun Google, GroqCloud, Ollama, Speechify, HuggingFace, ElevenLabs, and RapidAPI modules included!
* MULTIPLAYER: Netcode for GameObjects Host and Client option preconfigured out of the box!
* VOICE: Vivox Voice Services with Spatial audio (need to add your own credentials)
* DEBUG: In game Debug Console (3rd party free component) for getting debug information on standalone VR/XR systems
(* => excluding paid cloud services for the NPC and assumes you don't exceed the Vivox Voice services complementary service tresholds)

Specific Multiplayer Features
=============================
* Ad-hoc join capability with Just In Time client/server protocol and a server repository to synchronize prefabs across all clients post app launch
* 2-Stage Client Sign-In process with freedom to build and use your own rigged avatars (FBX), no need to register and package the avatars in Network Manager due to the included Client-Server-Join protocol and ServerRepository object 
* Hands, head and detailed finger tracking from your XR system to your avatars
* 4 preconfigured avatars (male/female, human/bot), easily replace with your own avatars
* Auto-IK component to automatically configure all IK constraints of your avatars hands, head and fingers
* Local or a web-based prefab/avatar resource repository
* Tool to generate asset bundles to store on the web-based repository
* First Person and Third Person mode via the UI (FP3P) with corresponding object grab methods

Specific AI Features
====================
* <b>AI Orchestrator</b> component to control which AI components you want to leverage
* <b>AI Text Filter</b> component to control what AI components to call using a basic lexical scanning mechanism using keywords
* Many <b>LLM's</b> supported hosted on Google, GroqCloud and even local (Ollama) - LLaMa, DeepSeek, Gemini, Gemma etc...
* 3 <b>Speech to Text</b> services supported: GroqCloud(OpenAI Whisper), HuggingFace(several inference models), ELevenlabs - in multiple languages
* 3 <b>Text to Speech</b> services supported: Speechify (Simba), ElevenLabs, RapidAPI - in multiple languages
* <b>Text to Image</b> experimental service on HuggingFace (Diffusion model)
* NPC code uses an API key storage. API keys are stored in Assets/Resources/Secure which is EXCLUDED from GitHub synchronisation (ie. in .gitignore)
* NPC has a <b>RAG</b> component which uses a local MariaDB/MySQL server to store pdf document chunks with their embeddings. This will require a Python script to load the documents into the database and generate embeddings and also a PHP script that implements a REST API for the RAG service. Please contact me if you are interested to implement RAG, I can provide the scripts and installation instructions (currently not yet integrated in this repository).

<b>What is new in this Branch - 20250611</b>
=====================================
* Added ElevenLabs STT component
* Sloyd Text to 3D service was deprecated as Sloyd disabled their API

Near Future Expected Updates
============================
* NPC AI VISION, this means that the NPC will be able to see its environment and send these images to an LLM (eg. Google Gemini Flash) and respond to what it sees
* NPC Web Search, ask the NPC to lookup information online using the Google Search API
* Addition of the RAG server side scripts - REST API script, SQL code to generate database and Python backend script

Steps to get started
====================
1. Pull/Fork the branch
2. Open Unity Editor, when prompted about Errors, select Ignore and do not start in safe mode
3. Go to File -> Open Scene -> BaMMain 
4. In the VivoxVoiceManager, enter your Vivox service credentials (if not used (yet), leave them blank)
5. Import the uLipSync package: Top Menu -> Assets -> Import Package -> Custom Package -> Navigate to the uLipSync package in this repository and import it
6. You may need to re-import the XR Interaction Toolkit samples and the XR Hands samples
7. If you want the InGame Debug Console then go to the Unity Store and purchase it (free)
8. If you want to talk with the NPC, be sure to get API keys from the following cloud providers: HuggingFace (free), GroqCloud (free), Speechify (paid), RapidAPI (partially free) and Sloyd.ai (partially free).
9. In Assets/Prefabs, click on the DynamicPrefabStarter object, select its ClientServerJoinProtocol script and ensure that UseWebRepository is UNCHECKED
10. Check the Platform setting under Unity Menu->File->Build Settings, if you want to compile for standalone VR (eg. Meta Quest 2,3) select Android otherwise select Windows
11. Open the Network Manager and change IP address of the host to the IP address of the device/PC that will act as the host
12. Create a new folder called Assets/Resources/Secure and create a file in it called APIKeys.txt with the following structure:
    * Google_API_Key:yourkeyhere
    * Groq_API_Key:yourkeyhere
    * Speechify_API_Key:yourkeyhere
    * HF_API_Key:yourkeyhere
    * ElevenLabs_API_Key:yourkeyhere
    * Rapid_API_Key:yourkeyhere
    * Sloyd_CLIENTID:yourkeyhere
    * Sloyd_CLIENTSECRET:yourkeyhere
13. Register with the AI services you want to use and generate AI keys, at least you will need: Speech to Text, LLM and a Text to Speech AI provider
14. Select the npcf GameObject in the Hierarchy panel and find the AI Orchestrator component, ensure that all AI components you want to leverage in the NPC are configured, ensure that ONLY ONE service is enabled per category (ie. select only one TTS provider otherwise you will hear 2 voices!). Add/Remove AI components to the npcf GameObject as desired. All components are available in Assets/Scripts/AI.

If you want to use remote prefabs
=================================
1. In Assets/Prefabs, click on the DynamicPrefabStarter object, select its ClientServerJoinProtocol script and ensure that UseWebRepository is CHECKED
2. Enter the url of your own webserver where you want the AssetBundles to be stored
3. In the Assets menu in Unity, select Build Asset Bundles
4. When that is completed, open a File Explorer and find your Assets folder, there is an AssetBundle folder with a windows and an android subfolder
5. ZIP the AssetBundle folder and transfer it to your webserver, unpack under the the url you entered in step 2

Compatibility
=============
* Currently this code is tested for Windows and Meta Quest 2 and 3 (Pico 4 not yet!)
* If you want to add Mac with a Web Repository then you must update the Editor/AssetBundlesBuild script to generate Mac AssetBundles ...
* AND open Assets/Scripts/Network/ClientServerJoinProtocol.cs and add code in the OnNetworkSpawn() method (pretty straightforward)
* If you have another VR brand headset, you will need to configure that headset in Project Settings -> XR Plugin Management
