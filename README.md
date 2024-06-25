# Welcome to OpenIris!

OpenIris is an adaptable and user-friendly open-source framework for video-based eye-tracking by [Ocular-Motor lab \@ UC Berkeley](https://omlab.berkeley.edu/). It is developed in C# with modular design that allows further extension and customization through plugins for different hardware systems, tracking, and calibration pipelines. It can be remotely controlled via a network interface from other devices or programs. Eye movements can be recorded online from (up to 4) camera stream or offline post-processing recorded videos. Example plugins have been developed to track pupil, corneal reflections, and torsion to capture eye motion in 3-D. Currently implemented binocular pupil tracking pipelines can achieve frame rates of more than 500Hz. 

OpenIris has a user-freindly graphical interface, basic tracking algorithm and example pipeline for a quick start. OpenIris already implements a set of core features common to most eye-tracking systems. This architecture allows plugin developers to focus on the image processing techniques for tracking or the hardware integration without the need to implement the often more tedious requirements involving user interface, real-time multithreading, data storage, error handling, and configuration.

![OpenIrisUI](https://github.com/ocular-motor-lab/OpenIris/assets/1356893/0164ac5c-dc84-4233-bcf2-1469568b6292)


# Quick Start
>[!NOTE]
> OpenIris can only work on Windows 10 or later versions (for now!)

To quickly install and checkout OpenIris features, download [the latest version of the application](https://github.com/ocular-motor-lab/OpenIris/releases).
- Download the release zip file in the desired directory
- Unzip the folder
- Inside the release folder, open openiris.exe file
- The openiris main window will open. Select "Simulation" from the "Eye Tracking System" menu (default option) and hit Start Tracking.
- The default pupil, corneal reflection and torsion tracking pipeline will start.

This is just an example video. To explore all the great features of the OpenIris, please read the wiki.

> [!TIP]
> In some cases, Windows requires manual permission to enable the plugins to work. On the first window of the program, under the "Eye Tracking System" drop-down menu, there should be more than three options. If you don't see more than three options, you need to enable them manually. To enable the available plugins, go to the release folder and its subfolders, right-click on a dll file, and select more options. Under the security section, check "Unblock" and hit ok. Repeat for all dll files. 

![testdll copy](https://github.com/ocular-motor-lab/OpenIris/assets/56368456/78c4456b-dbb8-4ca9-9de5-8c3132f72073)

### Help Shape the Future of Open Iris Eye-Tracking
We are excited to invite you to contribute to Open Iris, an innovative open-source eye-tracking framework that is transforming the field of vision research and human-computer interaction. By participating in this collaborative project, you have the opportunity to work with a passionate community of developers, researchers, and enthusiasts dedicated to advancing eye-tracking technology. Whether you are a seasoned programmer, a UX designer, a researcher, or simply someone with a keen interest in eye tracking, your unique skills and insights are invaluable to us. Join us in pushing the boundaries of what is possible, enhancing accessibility, and creating cutting-edge solutions that benefit everyone. Together, we can make Open Iris the premier platform for eye-tracking development and research.

### Cite
If you use OpenIris, please cite the OpenIris paper:

Roksana Sadeghi, Ryan Ressmeyer, Jacob Yates, and Jorge Otero-Millan. 2024. Open Iris - An Open Source Framework for Video-Based Eye-Tracking Research and Development. In Proceedings of the 2024 Symposium on Eye Tracking Research and Applications (ETRA '24). Association for Computing Machinery, New York, NY, USA, Article 17, 1–7. [https://doi.org/10.1145/3649902.3653348](https://doi.org/10.1145/3649902.3653348)



