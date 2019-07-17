# Rock Paper Scissors - Building an MR App

## Concept and Goals

The core idea that led to the Rock-Paper-Scissors app was to implement a hand pose matching system. The necessity of such a system has become apparent while working with hand tracking in general and MRTK in particular.

The final spark that led to implementation was the introduction of hand-constrained menus in MRTK ([HandConstraint API docs](https://microsoft.github.io/MixedRealityToolkit-Unity/api/Microsoft.MixedReality.Toolkit.Experimental.Utilities.Solvers.HandConstraint.html)). While these menus are very useful, they have a tendency to pop up on the user's hand unintentionally, because the solver does not distinguish a flat hand from a pinch or any other specific gesture. Having a way to test the hand pose against a given configuration of joint positions would make the menu much more specific, similar to how the bloom gesture on the Hololens works.

Many other applications are possible for hand gestures like this:
* Using gestures to trigger actions without the need to show virtual buttons should be explored.
* Games could use hand gestures to indicate actions such as shooting or casting magic spells.
* Sign language training has real-world uses. That includes the hearing-impared, for whom heavy use of voice commands and audio feedback may be an obstacle in AR/VR.
* Divers also use hand signs to communicate under water. A training app could teach use of proper signals in a (relatively) safe virtual environment.

The method used for matching hand poses can be extended with relative ease to include motion over time. In this way the movement of hands could be used as an input method too, beyond just the placement of fingers.

Having decided on implementing the hand pose matching base system, it was a fun excercise to come up with a simple show case. The game "Rock-Paper-Scissors" is a good choice, due to it's simplicity as well as reliance on detecting hand poses quickly and accurately, which highlights the algorithms effectiveness.

## Selecting the Algorithm

Hand data on the Hololens and similar devices is provided in the form of about 24 position vectors for hand joints. The exact number depends on whether bones such as the thumb metacarpal bone or virtual "bones" such as the palm and wrist are included.

### Machine Learning?

For a software developer with experience in the field (which the author is not) it would likely be straightforward to build a machine learning system which takes these vectors as inputs and tries to find a matching known pose. However, to create a successful ML system requires large amounts of training data, every time a new gesture should be "learned". The more conventional method explained below requires very little effort to record a new target pose and seems to be quite effective. It also has the added advantage of detecting hand poses in arbitrary rotations, which can be a useful feature. Machine learning might be employed at some point in the future, but is not necessary for solving the task at this stage.

### Least=Squares Fitting

To determine if the set of hand joint positions at any given time matches an expected target pose the joint positions can be compared to a prerecorded list of vectors. A good way of judging the quality of a fit is to use the squared distance of each point to its corresponding target. The mean (averaged) squared error (MSE) is a useful way to determine if the point set is a good fit. The MSE is compared to a given error threshold (e.g. sqrt(MSE) < 1cm) to decide if a target pose has been matched.

When comparing a hand pose to an earlier recording it has to be expected that the data has been translated and rotated, especially when given in world space as opposed to body-centric camera space. Luckily there are elegant methods for finding both a translation and a rotation of the point set, such that the squared error sum becomes _minimal_. This is essentially a linear regression problem. A hands-on explanation can be found at [here](http://nghiaho.com/?page_id=671). For an in-depth description the reader is referred to the paper:
“Least-Squares Fitting of Two 3-D Point Sets”, Arun, K. S. and Huang, T. S. and Blostein, S. D,
IEEE Transactions on Pattern Analysis and Machine Intelligence, Volume 9 Issue 5, May 1987

At the heart of the method is the construction of the covariance matrix sum of corresponding vector distances wrt. the centroid position. This matrix is then decomposed into two rotations and a diagonal matrix using a Singular Value Decomposition (SVD). This yields the desired rotation for the best fit, while the centroid distance yields the translation. The translation and rotation can be useful information to determine where hands are pointing, while the MSE is a measure of overall match quality.

### Weight factors

With the basic algorithm as described in the previous section each hand joint position contributes the same to the overall error. This can be a problem for many hand poses where some fingers are more significant than others. For example the "scissors" pose in the rock-paper-scissors game prominently features the index and middle finger in a V-shape. However, the placement of the thumb, ring finger and pinky are much less important and can actually vary a lot from person to person.

To make the system more flexible while still reliably detecting such poses an additional set of weight factors was introduced. These weights are multiplied with the squared errors when forming the error sum for the MSE. The effect is that a pose is detected as long as the significant fingers are in the right place, while having more leeway for other fingers.

Weight factors have to be utilized carefully, so as to not make the pose too generic. Otherwise a pose may be matched by unrelated input poses quite easily. Intuitively this is also where "learning" might be employed to adjust weight factors on the recorded data using back-propagation. [#building-a-recording-tool] describes how the weight factors were ultimately defined.

## Building a Recording Tool

For testing and to get a visual understanding of the algorithm it was decided to build a simple tool using MRTK. This would allow the user to record the current hand pose as a target pose, and then visualize the deviation of each joint as the hand is moved.

The first step was to implement the [IMixedRealityHandJointHandler](xref:Microsoft.MixedReality.Toolkit.Input.IMixedRealityHandJointHandler) in order to get the joint positions. The [IMixedRealitySourceStateHandler](xref:Microsoft.MixedReality.Toolkit.Input.IMixedRealitySourceStateHandler) was implemented as well to detect when hands start and stop tracking. Both of these interfaces are implemented by the `HandTracker` component, which has also been re-used for the Rock-Paper-Scissors game later.

`PoseRecorder` is a component that manages a recorded target pose. It visualizes the match quality of the current hand data to that target pose using overlayed sphere meshes. A text field shows the MSE value as an indication of the match quality.

The recorder component has a few public functions that allow hooking it up to Unity events. This is used for controlling the recorder using voice commands. A standard SpeechInputHandler pipes MRTK voice events into Unity events, which requires minimal effort by the recoder component.

An important aspect of recording the hand poses is the ability to save and load them to and from files. The poses are stored in a simple JSON format. The target position vectors and the associated weight factors are serialized, as well as (optionally) a list of identifier strings to make sure a pose can still be loaded in case the semantics of recorded points change.