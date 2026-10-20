plugins {
    id("com.android.library")
}

android {
    namespace = "com.google.firebase.messaging.cpp.stub"
    compileSdk = 34

    defaultConfig {
        minSdk = 21
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_1_8
        targetCompatibility = JavaVersion.VERSION_1_8
    }
}

dependencies {
    compileOnly("com.google.firebase:firebase-messaging:24.0.3")
}
