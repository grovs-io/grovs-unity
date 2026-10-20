plugins {
    id("com.android.library")
    id("org.jetbrains.kotlin.android")
}

android {
    namespace = "io.grovs.unity.firebase"
    compileSdk = 34

    defaultConfig {
        minSdk = 21
        consumerProguardFiles("consumer-rules.pro")
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_1_8
        targetCompatibility = JavaVersion.VERSION_1_8
    }

    kotlinOptions {
        jvmTarget = "1.8"
    }

    testOptions {
        unitTests.isIncludeAndroidResources = true
    }
}

dependencies {
    compileOnly(project(":firebase-stub"))
    compileOnly("com.google.firebase:firebase-messaging:24.0.3")
    compileOnly("io.grovs:Grovs:3.1.0")

    testImplementation(project(":firebase-stub"))
    testImplementation("com.google.firebase:firebase-messaging:24.0.3")
    testImplementation("io.grovs:Grovs:3.1.0")
    testImplementation("androidx.test:core:1.5.0")
    testImplementation("junit:junit:4.13.2")
    testImplementation("org.robolectric:robolectric:4.12.2")
}
