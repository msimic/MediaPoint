#pragma once

#ifndef _PLATERECOGNITION_H
#define _PLATERECOGNITION_H

#ifdef PLATERECOGNITION_EXPORTS
#define PLATE_RECOGNITION_API __declspec(dllexport)
#else
#define PLATE_RECOGNITION_API __declspec(dllimport)
#endif

#define EXTERN_C     extern "C"

#define API_VERSION 1
#define NO_VALUE -1000000000

//Nationalities
enum Nationality
{
    Nationality_Unknown,
    Nationality_Norway,
    Nationality_Sweden,
    Nationality_Sweden_PersonalPlate,
    Nationality_Denmark,
    Nationality_Germany,
    Nationality_Lithuania,
    Nationality_Estonia,
    Nationality_Poland,
    Nationality_Finland,
    Nationality_Latvia,
    Nationality_Spain,
    Nationality_GB,
    Nationality_Czech,
    Nationality_Netherlands,
    Nationality_Romania,
    Nationality_Bulgaria,
    Nationality_Belgium
};

//represents a single nationality alternative
struct NationalityAlternative
{
public:
	// nationality enum
	Nationality Nationality;
    // country recognition confidence. Value range in 1-1000.
    int NationalityConfidence;
	// description of plate subtype
	const wchar_t* PlateType;
	// unsafe result. For example personal plate.
	bool IsUnsafePlateType;
	// recognized text
	const wchar_t* OCRText;
	// OCR result confidence.
	int OCRConfidence;
	// country code string
    const wchar_t* CountryCode;
};

//OCR result return code
enum ReturnCode
{
    NoPlateFound = 0,
    DetectionSuccessful = 1,
    RejectedByOcrThreshold = 2,
    RejectedByRuleThreshold = 3,
    PlateNotReadable = 4,
};

//Confidence of a single character
struct CharConfidence
{
public:
	// The character
	wchar_t Char;
	// Confidence of the character
	int Confidence;
};

//List of characters and their confidences.
struct CharConfidences
{
public:
	// Pointer to contained items. Do not index it with an index higher then Count-1.
	CharConfidence* Items;
	// Count of items in this container
	int Count;
};

//List of nationality alternatives.
struct Nationalities
{
public:
	// Pointer to contained items. Do not index it with an index higher then Count-1.
	NationalityAlternative* Items;
	// Count of items in this container
	int Count;
};

//Shape definition of a licence plate
struct Shape
{
public:
	//left coordinate of bounding rectangle 
	int Left;
	//top coordinate of bounding rectangle 
	int Top;
	//right coordinate of bounding rectangle 
	int Right;
	//bottom coordinate of bounding rectangle 
	int Bottom;
	//top coordinate of bounding rectangle in deskewed space
	int TopTransformed;
	//bottom coordinate of bounding rectangle in deskewed space
	int BottomTransformed;
	//Expected horizontal skew angle of the plate in degrees. Positive angle means that image must be rotated clockwise.
	int AngleExpected;
	//Detected horizontal skew angle of the plate in degrees. Positive angle means that image must be rotated clockwise.
    //NO_VALUE if detection was not executed.
	double AngleDetectedHorizontal;
	//Detected vertical skew angle of the plate in degrees. Positive angle means that image must be rotated clockwise.
    //NO_VALUE if detection was not executed.
	double AngleDetectedVertical;	
	//character height in pixels
	int CharHeight;
};

//Single position of a licence plate candidate
struct LicencePlateCandidate
{
public:
	//Confidence of candidate. Value range is 1-1000.
	int Confidence;
	//Position coordinates and angle values
	Shape Shape;
};

//LicencePlateOCRResult object interface
struct ILicencePlateOCRResult
{
	//Gets the return code of this recognition
	virtual ReturnCode GetReturnCode() = 0;
	//Gets the recognized text for this result
	virtual const wchar_t* GetText() = 0;
	//Gets the confidence of this result
	virtual int GetConfidence() = 0;
	//Gets the confidences of each character that is part of the result text
	virtual CharConfidences GetCharConfidences() = 0;
	//Gets the best plate position candidate for this result
	virtual LicencePlateCandidate GetPlatePosition() = 0;
	//list of recognized country candidates
	virtual Nationalities GetNationalities() = 0;
	//Get the processing time
	virtual int GetProcessingTime() = 0;
	//Release this object from memory
	virtual void Release() = 0;
};

//LicencePlateOCRResult object
typedef ILicencePlateOCRResult* LicencePlateOCRResultHandle;

// Factory function that creates instances of the LicencePlateOCRResult object.
EXTERN_C PLATE_RECOGNITION_API LicencePlateOCRResultHandle APIENTRY CreateLicencePlateOCRResult(void);

//Image object interface
struct IImage
{
	// Initialize the image with Gray8 pixel data and the image properties
    virtual void Initialize(unsigned char*, int width, int height, int stride) = 0;
	//Gets the width of the image
	virtual int GetWidth() = 0;
	//Gets the height of the image
	virtual int GetHeight() = 0;
	//Gets the stride of the image
	virtual int GetStride() = 0;
	//Gets a pointer to the pixel data of the image
	virtual unsigned char* GetData() = 0;
	//Release this object from memory
    virtual void Release() = 0;
};

//Image object
typedef IImage* ImageHandle;

// Factory function that creates instances of the Image object.
EXTERN_C PLATE_RECOGNITION_API ImageHandle APIENTRY CreateImage(void);

// LicencePlateDetectionParameters object interface
struct ILicencePlateDetectionParameters
{
	// Expected angle: If the plate requires clockwise rotation to streighten horizontaly the angle must be positive.
    // If the plate requires counterclockwise rotation to streighten horizontaly the angle must be negative.
	virtual int GetPositionCandidateAngle() = 0;
    // Minimal expected character height in pixels.
    virtual int GetMinCharHeight() = 0;
    // Maximal expected character height in pixels.
    virtual int GetMaxCharHeight() = 0;
    // Confidence threshold for position candidates.
    // Confidence is in range 0-1000.
    // Default threshold is 300.
    virtual int GetPositionCandidateThreshold() = 0;
    // Expected top image margin in pixels.
    // Top margin is the image area where the plate is not possible.
    virtual int GetImageMarginTop() = 0;
    // Expected bottom image margin in pixels.
    // Top margin is the image area where the plate is not possible.
    virtual int GetImageMarginBottom() = 0;
    // Retry the detection and recognition with the gamma corrected if previous result is bad.
    virtual bool GetRetryGamma() = 0;
    // Retry the detection with the ±2° if previous result is bad.
    virtual bool GetRetryAngles() = 0;
	// Initialize values of parameters: read comments of Get methods for information on these parameters. Returns true if the initialization succeded. False if some parameters did not validate.
	virtual bool Initialize(int positionCandidateAngle, int minCharHeight, int maxCharHeight, int positionCandidateThreshold, int imageMarginTop, int imageMarginBottom, bool retryGamma, bool retryAngles) = 0;
	// Release this object from memory
    virtual void Release() = 0;
};

//LicencePlateDetectionParameters object
typedef ILicencePlateDetectionParameters* LicencePlateDetectionParametersHandle;

// Factory function that creates instances of the LicencePlateDetectionParameters object.
EXTERN_C PLATE_RECOGNITION_API LicencePlateDetectionParametersHandle APIENTRY CreateLicencePlateDetectionParameters();

// OCR behaviour types
enum OcrBehaviour
{
    CallCorrelationOcrNever,
    CallCorrelationOcrAlways,
    CallCorrelationOcrOnDemand,
};

//LicencePlateRecognitionParameters object interface
struct ILicencePlateRecognitionParameters
{
	//Get OCR behaviour definition
	virtual OcrBehaviour GetOcrBehaviour() = 0;
	//OCR behaviour definition.
    // Correlation OCR functionality will be executed based on this property.
	virtual void SetOcrBehaviour(OcrBehaviour) = 0;
	// List of forbidden results. Set value Nationality_Unknown to supress the specified plate text for all countries.
	virtual void AddResultExclusion(wchar_t* plateText, Nationality nationality) = 0;
	// Release this object from memory
    virtual void Release() = 0;
};

//LicencePlateRecognitionParameters object
typedef ILicencePlateRecognitionParameters* LicencePlateRecognitionParametersHandle;

// Factory function that creates instances of the LicencePlateRecognitionParameters object.
EXTERN_C PLATE_RECOGNITION_API LicencePlateRecognitionParametersHandle APIENTRY CreateLicencePlateRecognitionParameters();

//Processor object interface
struct IProcessor
{
	// Detect and recognize with default parameters
    virtual LicencePlateOCRResultHandle DetectAndRecognizePlate(ImageHandle) = 0;
	// Detect and recognize with custom parameters
	virtual LicencePlateOCRResultHandle DetectAndRecognizePlate(ImageHandle, LicencePlateDetectionParametersHandle, LicencePlateRecognitionParametersHandle) = 0;
	// Release this object from memory
    virtual void Release() = 0;
};

//Processor object
typedef IProcessor* ProcessorHandle;

// Factory function that creates instances of the Processor object.
EXTERN_C PLATE_RECOGNITION_API ProcessorHandle APIENTRY CreateProcessor(wchar_t* username, wchar_t* company, wchar_t* expiration_date, wchar_t* checksum, wchar_t* checksum2, Nationality nationality);

#endif /* !_PLATE_RECOGNITION_H */