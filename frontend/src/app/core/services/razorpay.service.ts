import { Injectable } from '@angular/core';

declare var Razorpay: any;

@Injectable({ providedIn: 'root' })
export class RazorpayService {
  private readonly keyId = 'rzp_test_Sk0wCWNzoiQKLF';

  openPaymentModal(options: {
    amount: number;
    policyName: string;
    customerName: string;
    customerEmail: string;
    description: string;
    onSuccess: (response: any) => void;
    onFailure: (error: any) => void;
  }): void {
    const razorpayOptions = {
      key: this.keyId,
      amount: Math.round(options.amount * 100),
      currency: 'INR',
      name: 'SmartSure Insurance',
      description: options.description,
      handler: (response: any) => {
        options.onSuccess(response);
      },
      prefill: {
        name: options.customerName,
        email: options.customerEmail,
        contact: '9999999999'
      },
      notes: {
        policy: options.policyName
      },
      theme: {
        color: '#1a56db'
      },
      modal: {
        ondismiss: () => {
          options.onFailure({ message: 'Payment cancelled by user' });
        }
      }
    };

    const rzp = new Razorpay(razorpayOptions);

    rzp.on('payment.failed', (response: any) => {
      options.onFailure(response);
    });

    rzp.open();
  }
}
