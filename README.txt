

Short Description:   Developer Friendly Unity AudioSource Management System


Description:

I struggled with handling audio sources, so I built a tool to lift that weight off my shoulders—both for now and for future projects.
Along the way, I encountered many interesting and challenging edge cases, and solving them taught me a great deal about the developer mindset. 
Initially, my goal was to create an absolute foolproof, plug-and-play solution. 
However, during the process, I realized that having a bit of control here and there is actually a good thing. This audio tool is the result.

Key Features:

 - Highly Optimized & Allocation-Free (Zero-GC)
 	- Engineered specifically for high-performance gameplay loops. 
	- Zero runtime allocations (GC.Alloc) when playing, stopping, or managing spatial audio. 

 - SOLID Architecture
 	- Strict separation of logic and state utilizing Pure C# Services.
	- Loose coupling between components ensures maximum maintainability and easy extensibility.

 - Hybrid Asynchronous Switch (USE_UNITASK)
 	- Automated compiler gating via Assembly Definition (ADF) symbols.
	- Leverages ultra-lightweight, allocation-free UniTaskVoid processes when UniTask is present.
	- Seamlessly falls back to cached, native Unity Coroutines if UniTask is missing.

 - Stateless Event Bus & O(1) Lookups
	- The central audio bus acts as a completely stateless relay station for static events.
	- Long-running sounds (loops) hand out a lightweight ticket struct (AudioHandle_Test) to the caller.
	- Allows stopping active sounds in \(O(1)\) time complexity, eliminating heavy search operations.

 - Intelligent Pooling & OneShot Lock Protection
	- Dynamically configurable, pre-cached AudioSource pool.
	- Automatic slot locking for short sound effects via timestamps (BusyUntilTime), preventing audio clipping or premature cutting.

 - Dynamic, Physical Wall-Occlusion Check (useWallCheck)
	- Opt-in obstacle detection between the player (AudioListener) and the sound source.
	- Resource-friendly raycasting restricted precisely to the player's position.
	- Dynamic frequency damping via an AudioLowPassFilter based on the specific layer hit.

 - Automated LayerMask Generation
	- Automatically generates the required LayerMask for the wall check at startup using bitshift operations from the user's frequency dictionary.

 - Global Audio State Control
	- Centralized event system to pause (PauseAll) and resume (UnpauseAll) all active sound sources within the pool simultaneously.