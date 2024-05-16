# Welcome to OpenIris!👋

OpenIris is an adaptable and user-friendly open-source framework for video-based eye-tracking by [Ocular-Motor lab @ UC Berkeley](https://omlab.berkeley.edu/). It is developed in C# with modular design that allows further extension and customization through plugins for different hardware systems, tracking, and calibration pipelines. It can be remotely controlled via a network interface from other devices or programs. Eye movements can be recorded online from (up to 4) camera stream or offline post-processing recorded videos. Example plugins have been developed to track pupil, corneal reflections, and torsion to capture eye motion in 3-D. Currently implemented binocular pupil tracking pipelines can achieve frame rates of more than 500Hz. 

OpenIris has a user-freindly graphical interface, basic tracking algorithm and example pipeline for a quick start. OpenIris already implements a set of core features common to most eye-tracking systems. This architecture allows plugin developers to focus on the image processing techniques for tracking or the hardware integration without the need to implement the often more tedious requirements involving user interface, real-time multithreading, data storage, error handling, and configuration.


![OpenIrisUI](https://github.com/ocular-motor-lab/OpenIris/assets/1356893/0164ac5c-dc84-4233-bcf2-1469568b6292)

# Quick Start
To quickly install and checkout OpenIris features, download the latest version of the application from : 

### Cite
Cite the OpenIris paper presented and published in ETRA 2024:


