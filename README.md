[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![License: CC BY 4.0](https://img.shields.io/badge/License-CC_BY_4.0-lightgrey.svg)](https://creativecommons.org/licenses/by/4.0/)

# UnityUtilities
A collection of utilities that were useful in my work  

# Scripts
This repo contains the following list of scripts:

-[unity custom editor for json](https://github.com/BaekNothing/UnityUtilities/blob/main/CustomEditor/jsonEditUtility.cs)  
-- Place this script on Unity/Editor  
-- Than, you can find tab on editor Window "MyUtil > UserInfoUtility"  
-- check path and file name (work only dir "Resorces/Json")  

-[optimized Scroll View](https://github.com/BaekNothing/UnityUtilities/blob/main/UI/OptimizedScrollview.cs)  
-- this utility target only to vertical & top cell index 0 (top to bottom) 

-[UIComponentCashing](https://github.com/BaekNothing/UnityUtilities/blob/main/UI/UIComponentCashing.cs)
-- Intentionally designed to work properly only when the function that returns the component, the variable that you want to cache, and the name of the target object are matched  
-- (because human error may occur in the process of incorrectly entering the string if found as a "Find" function)
