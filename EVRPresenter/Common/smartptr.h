// SmartPtr.h
//
// Defines a smart pointer class that does not depend on any ATL headers

#pragma once

namespace MediaFoundationSamples
{


    // _NoAddRefOrRelease:
    // This is a version of our COM interface that dis-allows AddRef
    // and Release. All ref-counting should be done by the SmartPtr 
    // object, so we want to dis-allow calling AddRef or Release 
    // directly. The operator-> returns a _NoAddRefOrRelease pointer
    // instead of returning the raw COM pointer. (This behavior is the
    // same as ATL's CComPtr class.)
    template <class T>
    class _NoAddRefOrRelease : public T
    {
    private:
        STDMETHOD_(ULONG, AddRef)() = 0;
        STDMETHOD_(ULONG, Release)() = 0;
    };

    template <class T>
    class SmartPtr
    {
    public:

        // Ctor
        SmartPtr() : m_ptr(NULL)
        {
        }

        // Ctor
        SmartPtr(T *ptr)
        {
            m_ptr = ptr;
            if (m_ptr)
            {
                m_ptr->AddRef();
            }
        }

        // Copy ctor
        SmartPtr(const SmartPtr& sptr)
        {
            m_ptr = sptr.m_ptr;
            if (m_ptr)
            {
                m_ptr->AddRef();
            }
        }

        // Dtor
        ~SmartPtr() 
        { 
            if (m_ptr)
            {
                m_ptr->Release();
            }
        }

        // Assignment
        SmartPtr& operator=(const SmartPtr& sptr)
        {
            // If we are assigned to ourselves, do nothing.
            if (!AreComObjectsEqual(m_ptr, sptr.m_ptr))
            {
                if (m_ptr)
                {
                    m_ptr->Release();
                }

                m_ptr = sptr.m_ptr;
                if (m_ptr)
                {
                    m_ptr->AddRef();
                }
            }
            return *this;
        }

        // address-of operator
	    T** operator&()
	    {
		    return &m_ptr;
	    }

        // dereference operator
        _NoAddRefOrRelease<T>* operator->()
        {
            return (_NoAddRefOrRelease<T>*)m_ptr;
        }

        // coerce to underlying pointer type.
        operator T*()
        {
            return m_ptr;
        }

        // Templated version of QueryInterface

        template <class Q> // Q is another interface type
        HRESULT QueryInterface(Q **ppQ)
        {
            return m_ptr->QueryInterface(__uuidof(Q), (void**)ppQ);
        }

        // safe Release() method
        ULONG Release()
        {
            T *ptr = m_ptr;
            ULONG result = 0;
            if (ptr)
            {
                m_ptr = NULL;
                result = ptr->Release();
            }
            return result;
        }

	    // Attach to an existing interface (does not AddRef)
	    void Attach(T* p) 
	    {
		    if (m_ptr)
            {
			    m_ptr->Release();
            }
		    m_ptr = p;
	    }


            
	    // Detach the interface (does not Release)
        T* Detach()
        {
            T* p = m_ptr;
            m_ptr = NULL;
            return p;
        }


        // equality operator
        bool operator==(T *ptr) const
        {
            return AreComObjectsEqual(m_ptr, ptr);
        }

	    bool operator!=(T* ptr) const
	    {
		    return !operator==(ptr);
	    }

    private:
        T *m_ptr;
    };

}; // namespace MediaFoundationSamples