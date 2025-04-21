import { Component, OnInit } from '@angular/core';
import { loadStripe, Stripe, StripeElements, StripeCardElement } from '@stripe/stripe-js';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-payment',
  templateUrl: './payment.component.html',
  styleUrls: ['./payment.component.scss']
})
export class PaymentComponent implements OnInit {
  stripe: Stripe | null = null;
  elements: StripeElements | null = null;
  card: StripeCardElement | null = null;

  stripePromise = loadStripe('pk_test_XXXXXXXXXXXXXXXXXXXXXXXX'); // Replace with your publishable key

  constructor(private http: HttpClient) {}

  async ngOnInit() {
    this.stripe = await this.stripePromise;
    if (this.stripe) {
      this.elements = this.stripe.elements();
      this.card = this.elements.create('card');
      this.card.mount('#card-element');
    }
  }

  async ngAfterViewInit() {
    const stripe = await this.stripePromise;
    if (stripe) {
      const elements = stripe.elements();
      this.card = elements.create('card');
      this.card.mount('#card-element');
    } else {
      console.error('Stripe failed to load.');
    }
  }

  async handlePayment(amount: number) {
    const stripe = await this.stripePromise;

    // Call your backend to create a PaymentIntent
    const response = await this.http
      .post<{ clientSecret: string }>('/api/payment/create-payment-intent', { amount })
      .toPromise();

    // Confirm the payment
    if (!stripe) {
      console.error('Stripe is not initialized.');
      return;
    }
    if (!response) {
      console.error('Failed to create PaymentIntent.');
      return;
    }
    const { error } = await stripe.confirmCardPayment(response.clientSecret, {
      payment_method: {
        card: this.card!,
      },
    });

    if (error) {
      console.error(error);
    } else {
      alert('Payment successful!');
    }
  }
}