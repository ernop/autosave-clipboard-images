# Clipboard Monitor and Saver Technical Specification

## 1. Overview

The Clipboard Monitor and Saver is a powershell script that monitors the clipboard for changes and saves the copied text and images to separate files. It includes features such as avoiding repeated saves of the same content, generating unique filenames, managing file storage, and cleaning and restoring the clipboard.

## 2. Functional Requirements

### 2.1 Clipboard Monitoring

- The script continuously monitors the clipboard for changes.
- It detects when new text or image data is copied to the clipboard.

### 2.2 Saving Text

- When text is copied to the clipboard, the script checks if it is different from the previously saved text.
- If the new text is different, it generates a unique filename based on the prefix of the text (but only alphanumeric characters count) plus a timestamp.
- The text is saved to a text file with the generated filename.
- The text is left in the clipboard for the user to continue, but internally, the script also keeps a copy of that text, not just relying on the version in the clipboard.

### 2.3 Saving Images

- When an image is copied to the clipboard, the script checks if it is different from any previously saved image, by hash, to avoid duplicates.
- If the new image is different, it generates a unique filename based on the current timestamp.
- The image is saved to a file with the generated filename.
- The script then restores the last copied text (if any) to the clipboard. That way the image doesn't stay in the clipboard.  i.e. The user copies text, you store that. Then, when the user copies an image, you save that, then remove it from the clipboard and restore the prior text.
- The script adds this image hash to the in-memory list, so that we won't save this one again, this session, to use in future comparisons.

### 2.4 File Storage

- The script saves the text and image files to a specified folder on the local file system.
- If the folder does not exist, the script creates it.
- The generated filenames follow a consistent format to ensure uniqueness and easy identification.
- The filename of images should start with just the timestamp, then .png or whatever the image format is.
- The filename of saved text should start with the first 20 characters of the text, followed by the timestamp, then .txt.
- The default save folder location is c:\Screenshots. There is no need for configuration powers of this.

### 2.5 Avoiding Repeated Saves

- The script uses hash comparisons to check if the current text or image is the same as the previously saved content.
- For text, it computes the hash of the copied text and compares it with any previously seen text hashes from this hash.
- For images, it calculates the hash of the copied image and compares it with any previously saved image hashes.
- If the hashes match, the content is considered the same, and no further action is taken.
- Same thing for text hashes. The point is we don't want to save the same image twice, nor the text twice.
- The script can keep hundreds of hashes it's seen this session in memory, but there is no long-term storage for this; every time the script is started, the list of known hashes is cleared, and that's fine.

### 2.6 Clipboard Cleaning and Restoration

- When saving an image, the script cleans the clipboard by setting the last saved text as the clipboard content using `[System.Windows.Forms.Clipboard]::SetText($global:lastText)`.
- This ensures that the original text is restored to the clipboard after copying an image.

## 3. Implementation Details

### 3.1 Programming Language and Environment

- The script is implemented in PowerShell, a scripting language available on Windows systems.
- It utilizes the .NET Framework classes `System.Windows.Forms` and `System.Drawing` for clipboard operations and image handling.

### 3.3 Configuration

- The script checks if the specified folder exists, and if not, it creates the folder.

### 3.4 Error Handling

- The script should handle and log any errors that occur during clipboard monitoring, file operations, or other critical tasks.
- Appropriate error messages or exceptions should be raised to assist in troubleshooting and debugging.

### 3.5 Performance Considerations

- The script should be efficient and responsive to clipboard changes, but it should not excessively consume system resources.
- A suitable interval or event-driven approach should be used to balance performance and responsiveness.
- Performance optimizations, such as multi-threading or event-driven programming techniques, can be explored to improve the responsiveness of the script.

## 4. Future Enhancements

The following features and improvements could be considered for future iterations of the Clipboard Monitor and Saver script:

- Support for monitoring additional data formats beyond text and images.
- Configurable options for file naming conventions, storage location, and other settings.
- Integration with cloud storage services for automatic backup or synchronization of saved files.
- Logging functionality to record clipboard changes, saved files, and any errors or exceptions.
- User interface enhancements, such as system tray notifications or a graphical interface for configuration and interaction.
- Support for different platforms or operating systems through cross-platform scripting languages or frameworks.

## 5. Conclusion

The Clipboard Monitor and Saver script provides a solution for automatically saving copied text and images to separate files while avoiding repeated saves and managing the clipboard. The technical specification outlines the functional requirements, implementation details, and potential future enhancements for the script.