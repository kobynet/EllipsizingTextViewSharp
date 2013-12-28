EllipsizingTextViewSharp
========================

A direct port of [EllipsizingTextView](https://github.com/triposo/barone/blob/master/src/com/triposo/barone/EllipsizingTextView.java) to C# to work with [Xamarin.Android](http://xamarin.com/monoforandroid)

I'v added few changes of my own like fixing the TextView cut last line descent (for example the letter 'g' descent might be cut off in some situations).

Usage
-----

```xml
    <?xml version="1.0" encoding="utf-8"?>
    <FrameLayout xmlns:android="http://schemas.android.com/apk/res/android"
    android:id="@+id/frame2"
    android:layout_width="200px"
    android:layout_height="101px"
    >
    <com.kobynet.text.EllipsizingTextViewSharp
      android:id="@+id/text1"
      android:layout_width="200px"
      android:layout_height="match_parent"
      android:text="bla bla bla bla bla bla bla blaaaaaaaaaaa ggg ggg gggggg ggg gggggg ggg ggg ggg ggg ggg"
      android:textSize="21px"
      />
    </FrameLayout>
```

Thanks to
---------
* [Triposo](https://github.com/triposo) - The creator of [EllipsizingTextView](https://github.com/triposo/barone/blob/master/src/com/triposo/barone/EllipsizingTextView.java)

TODO
----
* Not much, Ideas/Pull-requests are much welcome

License
-------
Just like the original this port is licensed under Apache 2.0.
    
    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at
    
    http://www.apache.org/licenses/LICENSE-2.0
    
    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.
