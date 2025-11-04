import { create } from 'zustand';
import type {
  ProviderProfileDto,
  ProfileValidationResultDto,
  ProfileRecommendationDto,
} from '../types/api-v1';

interface ProviderProfilesState {
  profiles: ProviderProfileDto[];
  activeProfile: ProviderProfileDto | null;
  recommendation: ProfileRecommendationDto | null;
  validationResults: Record<string, ProfileValidationResultDto>;
  loading: boolean;
  error: string | null;

  setProfiles: (profiles: ProviderProfileDto[]) => void;
  setActiveProfile: (profile: ProviderProfileDto | null) => void;
  setRecommendation: (recommendation: ProfileRecommendationDto | null) => void;
  setValidationResult: (profileId: string, result: ProfileValidationResultDto) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
  reset: () => void;
}

const initialState = {
  profiles: [],
  activeProfile: null,
  recommendation: null,
  validationResults: {},
  loading: false,
  error: null,
};

export const useProviderProfilesStore = create<ProviderProfilesState>((set) => ({
  ...initialState,

  setProfiles: (profiles) => set({ profiles }),
  
  setActiveProfile: (activeProfile) => set({ activeProfile }),
  
  setRecommendation: (recommendation) => set({ recommendation }),
  
  setValidationResult: (profileId, result) =>
    set((state) => ({
      validationResults: {
        ...state.validationResults,
        [profileId]: result,
      },
    })),
  
  setLoading: (loading) => set({ loading }),
  
  setError: (error) => set({ error }),
  
  reset: () => set(initialState),
}));
