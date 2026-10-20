plugins {
    id("com.android.library")
    id("org.jetbrains.kotlin.android")
}

android {
    namespace = "io.grovs.unity"
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
    compileOnly("io.grovs:Grovs:3.1.0")
    compileOnly("androidx.startup:startup-runtime:1.1.1")
    compileOnly("androidx.lifecycle:lifecycle-process:2.6.2")

    testImplementation("io.grovs:Grovs:3.1.0")
    testImplementation("androidx.startup:startup-runtime:1.1.1")
    testImplementation("androidx.lifecycle:lifecycle-process:2.6.2")
    testImplementation("androidx.test:core:1.5.0")
    testImplementation("junit:junit:4.13.2")
    testImplementation("org.robolectric:robolectric:4.12.2")
    testImplementation("org.json:json:20231013")
}
