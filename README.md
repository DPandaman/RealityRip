# RealityRip | TreeHacks 2026



## Objective
A drone flight simulator that combines high-fidelity Gaussian Splatting digital twin environments with a generative AI commentator. Our goal was to train fpv drone pilots in digital twins of real environments they’d fly in, and give them live feedback to keep them engaged. Using an iPhone, RealityRip can reconstruct a room or building into a navigable 3D model within minutes. The generated digital twin can be flown through immediately or modified to simulate obstacles, hazards, or mission scenarios.

## Motivation 
In search-and-rescue operations, pilots must navigate unfamiliar, cluttered, and often dangerous environments under extreme time pressure. Preparing for these conditions is hard because of unrealistic and limited map selection, mainly due to the effort required to 3d model a training course. RealityRip was created to fix that.

We wanted to build a training system that replicates the spatial complexity of real environments while actively guiding pilots as they learn. By reconstructing real-world spaces using Gaussian Splatting, we transform homes, buildings, and disaster zones into high-fidelity digital twins. Pilots can train in environments that mirror the unpredictability and constraints of real missions without having to fear not having practiced beforehand..

## Features
- Gaussian Splatting to generate a fully 3d, textured, model of your world from just an iphone
- Both Local and Cloud AI assistance
- Physics based FPV sim
### Gaussian Splatting
Gaussian Splatting is a technique to create a fully textured 3d model of a portion of the world solely off of video. It generates both extremely accurate depth maps and texture maps, but comes with the tradeoff of requiring a large amount of compute. For this project, because of our focus on Edge AI, we decided to augment the Gaussian Splatting with the iPhone's built in lidar camera to focus on reducing latency.

We use the iPhone’s built-in LIDAR sensor, a forked version of an iPhone app to transmit data to our ASUS computer, and the raw computing power of the ASUS Ascent GX10. We used SplaTAM, which is a Gaussian Splatting implementation that also takes advantage of the iPhone’s lidar. We decided to include the iPhone’s lidar in our splatting because it helped with stability, especially in darker rooms. It also greatly reduced the compute necessary to generate our splat.

We forked SplaTAM and the iPhone app NeRF Capture to get around an issue they had with wireless communication. These 2 projects talk to each other using multicast, which is a P2P networking protocol that is blocked by Eduroam. We forked both of these to add support for sending directly to an IP address, and added support for real time visualization of gaussian splats as well. 


### Local and Cloud AI integrations
We use both local (running on an ASUS Ascent GX10) and cloud (GPT-5.2) AI models, including VLMs, LLMs, and a text to speech model. GPT-5.2 is used as a VLM, and is used to help critique a flyers pathing to a given route. 

For more latency sensitive tasks, like generating audio feedback for the user, we use local models running on the ASUS Ascent GX10. We are using openai’s gpt-oss-120b for our LLM, which we chose due to its relatively light weight, and also native fp4 compute, which takes full advantage of the GX10’s best computational unit. For our text-to-speech (TTS) model, we are going with the newly released Chatterbox from resemble.ai. Our main focus here was to keep the latency down, while still having an extremely expressive and good sounding model. We’re super happy about using open models on the GX10 because of how much they reduce total latency in our system. 

### Physics-Driven FPV Controller
We used Unity to create our FPV sim, mainly because of the quality of its documentation. Unity has a plugin that allows it to directly render our .ply and .gltf files that our gaussian splatting creates, and also is a full game engine, which made creating our drone sim relatively easy. 
One of the most important parts of FPV sims is ensuring that they match the physics of the real world. We leaned on Unity’s RigidBody system, which gave us collision detection along with sensible gravity and acceleration.


## Setup & Installation
1. **Clone the repository.**
2. **Unity Version:** Open in Unity 2022.3+ or Unity 6.
3. **API Configuration:** Ensure you have your OpenAI API key set as an environment variable (OPENAI_API_KEY) on your system. The AIService.cs script will pull this automatically.
4. **Hardware:** Connect your RadioMaster Pocket via USB-C (Game Controller mode).
5. **Press Play** and fly!

Team Members:
* Devanshu Pandya (University of Illinois)
* Julia Jiang (Stanford University)
* Koichi Kimoto (Stanford University)
* Rohan Godha (Georgia-Tech)
