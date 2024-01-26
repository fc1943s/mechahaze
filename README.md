# MechaHaze

### Index
1. **Introduction**
   - Overview of MechaHaze
   - Purpose and Target Audience

2. **Features**
   - Audio Processing Capabilities
   - Frontend and Backend Integration
   - Real-Time Data Handling and Control
   - State Management and Persistence
   - Automated Testing

3. **Installation**
   - Prerequisites
   - Installation Steps

4. **Usage**
   - Basic Usage Instructions
   - Advanced Features

—

#### Introduction
**MechaHaze** is an advanced audio processing system designed to handle sophisticated audio analysis and real-time interaction. This project integrates cutting-edge technologies in both frontend and backend development to provide a comprehensive tool for audio processing, visualization, and control.

The primary aim of MechaHaze is to serve professionals and enthusiasts in the audio industry, offering a versatile platform for audio analysis, editing, and live performance setups. Its modular design allows for scalability and adaptability, catering to a wide range of audio processing needs.

#### Features

**Audio Processing Capabilities**:
MechaHaze excels in advanced audio processing functionalities, including:
- Audio recording and listening.
- Audio fingerprinting for recognition and categorization.
- Sophisticated waveform analysis for detailed audio data interpretation.

**Frontend and Backend Integration**:
- The frontend is built using Fable for a seamless F# to JavaScript compilation, featuring modern React patterns for a dynamic user interface.
- State management is efficiently handled using Recoil, enabling responsive and interactive UI components.

**Real-Time Data Handling and Control**:
- Incorporates Open Sound Control (OSC) for swift responsiveness in real-time audio processing environments.
- Utilizes RabbitMQ for robust and scalable inter-process communication, facilitating a distributed system architecture.


#### Installation

**Prerequisites**:
- .NET 5 SDK: MechaHaze is built with .NET 5, ensuring cross-platform compatibility.
- Node.js and npm/yarn: Required for managing frontend dependencies and scripts.
- Fable Compiler: For compiling F# source code to JavaScript for the frontend.
- RabbitMQ: Used for inter-process communication within the application.

**Installation Steps**:
1. **Clone the Repository**:
   ```
   git clone https://github.com/fc1943s/mechahaze.git
   cd mechahaze
   ```

2. **Install Backend Dependencies**:
   ```
   cd src/MechaHaze.Daemon
   dotnet restore
   ```

3. **Set Up the Frontend**:
   ```
   cd src/MechaHaze.UI.Frontend
   yarn install  # or npm install
   ```

4. **Compile the Frontend**:
   ```
   fable
   ```

5. **Start the Backend Services**:
   Navigate to the respective daemon or service directory and use:
   ```
   dotnet run
   ```

6. **Launch the Frontend Application**:
   ```
   yarn start  # or npm start
   ```

#### Usage

**Basic Usage Instructions**:
- Start the backend services before launching the frontend application.
- Access the web interface via `http://localhost:8080` (or the configured port) to interact with the application.
- Explore the various features such as audio recording, waveform analysis, and real-time control through the user interface.

**Advanced Features**:
- Customize audio processing parameters through the settings in the UI.
- Utilize the real-time data handling for live audio manipulation.
- Explore the extensive logging and state management capabilities for debugging or advanced configurations.


